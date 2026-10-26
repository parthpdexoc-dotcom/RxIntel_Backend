using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using AutoMapper;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Helpers;
using Rxintel.Generic.Services.Interfaces;
using Rxintel.Generic.Services.MetaAI;
using Microsoft.EntityFrameworkCore;

namespace Rxintel.Generic.Services.Services
{
    public class ErxAuditService : IErxAuditService
    {
        private readonly IErxAuditBatchRepository _batchRepository;
        private readonly IErxAuditRecordRepository _recordRepository;
        private readonly IErxColumnMatchRuleRepository _ruleRepository;
        private readonly IErxAuditDiscrepancyRepository _discrepancyRepository;
        private readonly IInsuranceCompanyRepository _insuranceCompanyRepository;
        private readonly IInsuranceRepository _insuranceRepository;
        private readonly IDeaScheduleService _deaScheduleService;
        private readonly IMapper _mapper;
        private readonly ILogger<ErxAuditService> _logger;
        private readonly ITokenParser? _tokenParser;
        private readonly IErxComparisonEngine? _comparisonEngine;
        private readonly RxintelDbContext? _dbContext;
        private readonly IErxReferenceDataService? _referenceDataService;
        private readonly IMetaAiClient? _metaAiClient;

        public ErxAuditService(
            IErxAuditBatchRepository batchRepository,
            IErxAuditRecordRepository recordRepository,
            IErxColumnMatchRuleRepository ruleRepository,
            IErxAuditDiscrepancyRepository discrepancyRepository,
            IInsuranceCompanyRepository insuranceCompanyRepository,
            IInsuranceRepository insuranceRepository,
            IMapper mapper,
            ILogger<ErxAuditService> logger,
            IDeaScheduleService? deaScheduleService = null,
            ITokenParser? tokenParser = null,
            IErxComparisonEngine? comparisonEngine = null,
            RxintelDbContext? dbContext = null,
            IErxReferenceDataService? referenceDataService = null,
            IMetaAiClient? metaAiClient = null)
        {
            _batchRepository = batchRepository;
            _recordRepository = recordRepository;
            _ruleRepository = ruleRepository;
            _discrepancyRepository = discrepancyRepository;
            _insuranceCompanyRepository = insuranceCompanyRepository;
            _insuranceRepository = insuranceRepository;
            _mapper = mapper;
            _logger = logger;
            _deaScheduleService = deaScheduleService ?? new DeaScheduleService();
            _tokenParser = tokenParser;
            _comparisonEngine = comparisonEngine;
            _dbContext = dbContext;
            _referenceDataService = referenceDataService;
            _metaAiClient = metaAiClient;
        }

        public async Task<PagedList<ErxAuditBatchDto>> GetBatchesAsync(PageParam pageParam, Guid? pharmacyId = null)
        {
            if (!pharmacyId.HasValue && _tokenParser != null && !_tokenParser.IsAdmin() && !_tokenParser.IsSuperAdmin())
            {
                pharmacyId = _tokenParser.GetPharmacyId();
            }

            _logger.LogInformation("[AuditService:GetBatches] Fetching batches (Search: '{SearchText}', PharmacyId: {PharmacyId}, Page: {Page}, PageSize: {PageSize}).",
                pageParam?.SearchText, pharmacyId, pageParam?.Page, pageParam?.PageSize);

            IQueryable<ErxAuditBatch> query = _dbContext != null
                ? _dbContext.ErxAuditBatches.Include(b => b.Pharmacy).AsNoTracking()
                : _batchRepository.GetAllQueryable().Include(b => b.Pharmacy).AsNoTracking();

            if (pharmacyId.HasValue && pharmacyId.Value != Guid.Empty)
            {
                query = query.Where(b => b.PharmacyId == pharmacyId.Value);
            }

            if (!string.IsNullOrWhiteSpace(pageParam?.SearchText))
            {
                var s = pageParam.SearchText.Trim();
                query = query.Where(b =>
                    (b.FileName != null && b.FileName.Contains(s)) ||
                    (b.UploadedBy != null && b.UploadedBy.Contains(s)) ||
                    (b.Status != null && b.Status.Contains(s)) ||
                    (b.Pharmacy != null && ((b.Pharmacy.BusinessTradeName != null && b.Pharmacy.BusinessTradeName.Contains(s)) ||
                                            (b.Pharmacy.BusinessLegalName != null && b.Pharmacy.BusinessLegalName.Contains(s)) ||
                                            (b.Pharmacy.NPI != null && b.Pharmacy.NPI.Contains(s)))));
            }

            var totalCount = await query.CountAsync();
            var pageNumber = pageParam?.Page.GetValueOrDefault(1) ?? 1;
            if (pageNumber < 1) pageNumber = 1;
            var pageSize = pageParam?.PageSize.GetValueOrDefault(10) ?? 10;
            if (pageSize < 1) pageSize = 10;

            var pagedQuery = query.OrderByDescending(b => b.UploadedAt);
            IQueryable<ErxAuditBatch> batchesToFetch = (pageParam != null && pageParam.IsPaginationRequested)
                ? pagedQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize)
                : pagedQuery;

            var pagedBatches = await batchesToFetch.ToListAsync();
            var dtos = _mapper.Map<List<ErxAuditBatchDto>>(pagedBatches);
            _logger.LogInformation("[AuditService:GetBatches] Total batches found: {TotalCount}, returning page of {Count} batches.", totalCount, dtos.Count);

            return new PagedList<ErxAuditBatchDto>(dtos, pageParam ?? new PageParam(), totalCount);
        }

        public async Task<ErxAuditBatchDto?> GetBatchByIdAsync(Guid batchId)
        {
            _logger.LogInformation("[AuditService:GetBatchById] Fetching batch for BatchId: {BatchId}.", batchId);
            ErxAuditBatch? batch;
            if (_dbContext != null)
            {
                batch = await _dbContext.ErxAuditBatches.Include(b => b.Pharmacy).AsNoTracking().FirstOrDefaultAsync(b => b.BatchId == batchId);
            }
            else
            {
                batch = await _batchRepository.GetAllQueryable().Include(b => b.Pharmacy).AsNoTracking().FirstOrDefaultAsync(b => b.BatchId == batchId);
            }
            _logger.LogInformation("[AuditService:GetBatchById] BatchId: {BatchId} -> {Status}.", batchId, batch != null ? $"Found ('{batch.FileName}')" : "Not Found");
            return batch == null ? null : _mapper.Map<ErxAuditBatchDto>(batch);
        }

        public async Task<SuccessResult<string>> DeleteBatchAsync(Guid batchId)
        {
            try
            {
                _logger.LogInformation("[AuditService:DeleteBatch] Deleting batch for BatchId: {BatchId}...", batchId);
                var batch = await _batchRepository.GetByIdAsync(batchId, "BatchId");
                if (batch == null)
                {
                    _logger.LogWarning("[AuditService:DeleteBatch] Batch {BatchId} not found.", batchId);
                    return SuccessResult<string>.NotFound("Batch not found.");
                }

                var fileName = batch.FileName;
                await _batchRepository.DeleteAsync(batchId);
                _logger.LogInformation("[AuditService:DeleteBatch] Batch {BatchId} ('{FileName}') deleted successfully.", batchId, fileName);
                return SuccessResult<string>.Ok(fileName, "Batch deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ERx audit batch {BatchId}: {Message}", batchId, ex.Message);
                return SuccessResult<string>.BadRequest($"Failed to delete batch: {ex.Message}");
            }
        }


        public Task<PagedList<ErxAuditRecordDto>> GetBatchRecordsAsync(
            Guid batchId,
            PageParam pageParam,
            bool? discrepanciesOnly = null,
            string? discrepancyColumn = null,
            int? ruleId = null,
            string? paymentMethod = null,
            string? insCode = null,
            string? binNo = null,
            decimal? minTotalPaid = null,
            decimal? maxTotalPaid = null,
            bool? isHighValue = null,
            string? rxOrigin = null,
            bool? nonElectronicOnly = null,
            Guid? pharmacyId = null)
        {
            return GetAllRecordsAsync(batchId, pageParam, discrepanciesOnly, discrepancyColumn, ruleId,
                paymentMethod, insCode, binNo, minTotalPaid, maxTotalPaid, isHighValue, rxOrigin, nonElectronicOnly, pharmacyId);
        }

        public async Task<PagedList<ErxAuditRecordDto>> GetAllRecordsAsync(
            Guid? batchId,
            PageParam pageParam,
            bool? discrepanciesOnly = null,
            string? discrepancyColumn = null,
            int? ruleId = null,
            string? paymentMethod = null,
            string? insCode = null,
            string? binNo = null,
            decimal? minTotalPaid = null,
            decimal? maxTotalPaid = null,
            bool? isHighValue = null,
            string? rxOrigin = null,
            bool? nonElectronicOnly = null,
            Guid? pharmacyId = null)
        {
            if (!pharmacyId.HasValue && _tokenParser != null && !_tokenParser.IsAdmin() && !_tokenParser.IsSuperAdmin())
            {
                pharmacyId = _tokenParser.GetPharmacyId();
            }

            _logger.LogInformation("[AuditService:GetAllRecords] Loading records (BatchId: {BatchId}, PharmacyId: {PharmacyId}, DiscrepanciesOnly: {DiscrepanciesOnly}, Column: '{Column}', RuleId: {RuleId}, PaymentMethod: '{PaymentMethod}', InsCode: '{InsCode}', BinNo: '{BinNo}', HighValue: {HighValue}, RxOrigin: '{RxOrigin}', NonElectronic: {NonElectronic}, Page: {Page}, PageSize: {PageSize}).",
                batchId, pharmacyId, discrepanciesOnly, discrepancyColumn, ruleId, paymentMethod, insCode, binNo, isHighValue, rxOrigin, nonElectronicOnly, pageParam?.Page, pageParam?.PageSize);

            // 1. Build Queryable with AsNoTracking for ultra-fast SQL-level execution
            IQueryable<ErxAuditRecord> query = _dbContext != null
                ? _dbContext.ErxAuditRecords.AsNoTracking()
                : _recordRepository.GetAllQueryable().AsNoTracking();

            // 2. Filter by BatchId if specified
            if (batchId.HasValue && batchId.Value != Guid.Empty)
            {
                query = query.Where(r => r.BatchId == batchId.Value);
            }

            if (pharmacyId.HasValue && pharmacyId.Value != Guid.Empty)
            {
                query = query.Where(r => r.Batch != null && r.Batch.PharmacyId == pharmacyId.Value);
            }

            // 3. Filter by RuleId, Column, or DiscrepanciesOnly at SQL level
            if (ruleId.HasValue && ruleId.Value > 0)
            {
                query = query.Where(r => r.Discrepancies.Any(d =>
                    (d.Status == "Open" || string.IsNullOrEmpty(d.Status)) &&
                    d.RuleId == ruleId.Value));
            }
            else if (!string.IsNullOrWhiteSpace(discrepancyColumn))
            {
                var targetCols = discrepancyColumn.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                if (targetCols.Count > 0)
                {
                    var allRules = await _ruleRepository.GetAllAsync(r => r.IsActive);
                    var targetRuleIds = allRules
                        .Where(r => targetCols.Any(col =>
                            string.Equals(r.FieldGroupName, col, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(r.MmColumnName, col, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(r.ErxColumnName, col, StringComparison.OrdinalIgnoreCase) ||
                            (r.FieldGroupName != null && r.FieldGroupName.IndexOf(col, StringComparison.OrdinalIgnoreCase) >= 0)))
                        .Select(r => r.RuleId)
                        .Distinct()
                        .ToList();

                    if (targetRuleIds.Count > 0)
                    {
                        query = query.Where(r => r.Discrepancies.Any(d =>
                            (d.Status == "Open" || string.IsNullOrEmpty(d.Status)) &&
                            targetRuleIds.Contains(d.RuleId)));
                    }
                    else
                    {
                        query = query.Where(r => false);
                    }
                }
            }
            else if (discrepanciesOnly == true)
            {
                query = query.Where(r => r.Discrepancies.Any(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status)));
            }

            // 4. Payment Method & Audit Priority Filter
            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                var pm = paymentMethod.Trim();
                if (pm.Equals("I", StringComparison.OrdinalIgnoreCase) || pm.Equals("Insurance", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.PaymentMethod == "I" || r.PaymentMethod == "Insurance");
                }
                else if (pm.Equals("C", StringComparison.OrdinalIgnoreCase) || pm.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.PaymentMethod == "C" || r.PaymentMethod == "Cash");
                }
                else
                {
                    query = query.Where(r => r.PaymentMethod == pm);
                }
            }

            // 5. Insurance Code Filter (supports single value or comma-separated multi-select)
            if (!string.IsNullOrWhiteSpace(insCode))
            {
                var codes = insCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (codes.Length == 1)
                {
                    var ic = codes[0];
                    query = query.Where(r => r.InsCode != null && (r.InsCode == ic || r.InsCode.Contains(ic)));
                }
                else if (codes.Length > 1)
                {
                    query = query.Where(r => r.InsCode != null && codes.Contains(r.InsCode));
                }
            }

            // 6. Insurance BIN Filter (supports single value or comma-separated multi-select)
            if (!string.IsNullOrWhiteSpace(binNo))
            {
                var bins = binNo.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (bins.Length == 1)
                {
                    var bn = bins[0];
                    query = query.Where(r => r.BinNo != null && (r.BinNo == bn || r.BinNo.Contains(bn)));
                }
                else if (bins.Length > 1)
                {
                    query = query.Where(r => r.BinNo != null && bins.Contains(r.BinNo));
                }
            }

            // 7. Total Amount Paid / High-Value Claims Filter
            if (minTotalPaid.HasValue)
            {
                query = query.Where(r => r.TotalInsPaid.HasValue && r.TotalInsPaid >= minTotalPaid.Value);
            }
            if (maxTotalPaid.HasValue)
            {
                query = query.Where(r => r.TotalInsPaid.HasValue && r.TotalInsPaid <= maxTotalPaid.Value);
            }
            if (isHighValue == true)
            {
                query = query.Where(r => r.TotalInsPaid.HasValue && r.TotalInsPaid >= 500m);
            }

            // 8. Rx Origin / Non-Electronic Hardcopy Comparison Flag Filter (supports single value or comma-separated multi-select)
            if (!string.IsNullOrWhiteSpace(rxOrigin))
            {
                var origins = rxOrigin.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (origins.Length == 1)
                {
                    var ro = origins[0];
                    query = query.Where(r => r.MmRxOrigin != null && (r.MmRxOrigin == ro || r.MmRxOrigin.Contains(ro)));
                }
                else if (origins.Length > 1)
                {
                    query = query.Where(r => r.MmRxOrigin != null && origins.Contains(r.MmRxOrigin));
                }
            }
            if (nonElectronicOnly == true)
            {
                query = query.Where(r => r.MmRxOrigin != null && r.MmRxOrigin.ToUpper() != "ELECTRONIC");
            }

            // 9. Filter by SearchText at SQL level
            if (!string.IsNullOrWhiteSpace(pageParam?.SearchText))
            {
                var s = pageParam.SearchText.Trim();
                query = query.Where(r =>
                    (r.RxNumber != null && r.RxNumber.Contains(s)) ||
                    (r.MmPatientName != null && r.MmPatientName.Contains(s)) ||
                    (r.ErxPatientName != null && r.ErxPatientName.Contains(s)) ||
                    (r.MmPrescriberName != null && r.MmPrescriberName.Contains(s)) ||
                    (r.ErxPrescriberName != null && r.ErxPrescriberName.Contains(s)) ||
                    (r.MmDispensedDrugName != null && r.MmDispensedDrugName.Contains(s)) ||
                    (r.ErxDrugName != null && r.ErxDrugName.Contains(s)) ||
                    (r.InsCode != null && r.InsCode.Contains(s)) ||
                    (r.BinNo != null && r.BinNo.Contains(s)) ||
                    (r.PaymentMethod != null && r.PaymentMethod.Contains(s)) ||
                    (r.MmRxOrigin != null && r.MmRxOrigin.Contains(s)) ||
                    (r.Batch != null && r.Batch.Pharmacy != null &&
                        ((r.Batch.Pharmacy.BusinessTradeName != null && r.Batch.Pharmacy.BusinessTradeName.Contains(s)) ||
                         (r.Batch.Pharmacy.BusinessLegalName != null && r.Batch.Pharmacy.BusinessLegalName.Contains(s)) ||
                         (r.Batch.Pharmacy.NPI != null && r.Batch.Pharmacy.NPI.Contains(s)))));
            }

            // 10. Total count computed in SQL without pulling data
            var totalCount = await query.CountAsync();

            // 11. Pagination pushed down to SQL Server (OFFSET ... FETCH NEXT) with customizable sorting
            var recPageNumber = pageParam?.Page.GetValueOrDefault(1) ?? 1;
            if (recPageNumber < 1) recPageNumber = 1;
            var recPageSize = pageParam?.PageSize.GetValueOrDefault(10) ?? 10;
            if (recPageSize < 1) recPageSize = 10;

            var sortBy = pageParam?.SortBy?.Trim().ToLower();
            var isAsc = pageParam?.SortDirection?.Trim().ToLower() == "asc";

            IQueryable<ErxAuditRecord> sortedQuery;
            if (sortBy == "binno" || sortBy == "bin")
            {
                sortedQuery = isAsc ? query.OrderBy(r => r.BinNo) : query.OrderByDescending(r => r.BinNo);
            }
            else if (sortBy == "totalpaid" || sortBy == "totalinspaid" || sortBy == "amountpaid")
            {
                sortedQuery = isAsc ? query.OrderBy(r => r.TotalInsPaid) : query.OrderByDescending(r => r.TotalInsPaid);
            }
            else if (sortBy == "paymentmethod" || sortBy == "priority")
            {
                sortedQuery = isAsc ? query.OrderBy(r => r.PaymentMethod) : query.OrderByDescending(r => r.PaymentMethod);
            }
            else if (sortBy == "inscode")
            {
                sortedQuery = isAsc ? query.OrderBy(r => r.InsCode) : query.OrderByDescending(r => r.InsCode);
            }
            else if (sortBy == "rxorigin")
            {
                sortedQuery = isAsc ? query.OrderBy(r => r.MmRxOrigin) : query.OrderByDescending(r => r.MmRxOrigin);
            }
            else
            {
                sortedQuery = query.OrderByDescending(r => r.RecordId);
            }

            IQueryable<ErxAuditRecord> pagedQuery = sortedQuery;
            if (pageParam == null || pageParam.IsPaginationRequested)
            {
                pagedQuery = pagedQuery.Skip((recPageNumber - 1) * recPageSize).Take(recPageSize);
            }

            // 7. Load child discrepancies and rules ONLY for the paged records (include Batch.Pharmacy for Tenant info)
            var paged = await pagedQuery
                .Include(r => r.Batch)
                    .ThenInclude(b => b.Pharmacy)
                .Include(r => r.Discrepancies)
                    .ThenInclude(d => d.Rule)
                .AsNoTracking()
                .ToListAsync();

            // 8. Auto-reconcile only the paged records in memory if any open discrepancies qualify
            var rulesDict = (await _ruleRepository.GetAllAsync()).ToDictionary(r => r.RuleId);
            var discrepanciesToPersist = new List<ErxAuditDiscrepancy>();

            foreach (var record in paged)
            {
                var hasMatchingDemographics = ErxComparisonEngine.ArePatientDemographicsMatching(record);
                var isControlled = (_referenceDataService?.IsControlledSubstance(record) ?? false) ||
                                   _deaScheduleService.IsClass2Narcotic(record) ||
                                   _deaScheduleService.IsClass3To5Narcotic(record);
                var hasMatchingNdc = ErxComparisonEngine.AreNdcsMatching(record);
                var isQtyOrderedCompliant = ErxComparisonEngine.IsQuantityOrderedCompliant(record);
                var isQtyDispensedCompliant = ErxComparisonEngine.IsQuantityDispensedCompliant(record, metaAiClient: _metaAiClient);
                var isDaysSupplyCompliant = ErxComparisonEngine.IsDaysSupplyCompliant(record, metaAiClient: _metaAiClient);

                foreach (var d in record.Discrepancies)
                {
                    if (d.Status != "Open" && !string.IsNullOrEmpty(d.Status)) continue;
                    if (d.Rule == null && rulesDict.TryGetValue(d.RuleId, out var ruleObj))
                    {
                        d.Rule = ruleObj;
                    }
                    if (d.Rule == null) continue;

                    var resolved = false;
                    var reason = "";

                    if (hasMatchingDemographics && ErxComparisonEngine.IsPatientNameRule(d.Rule))
                    {
                        resolved = true;
                        reason = "System (Demographics Match)";
                    }
                    else if (!isControlled && ErxComparisonEngine.IsDeaRule(d.Rule))
                    {
                        resolved = true;
                        reason = "System (Non-Controlled Drug)";
                    }
                    else if (hasMatchingNdc && (ErxComparisonEngine.IsDrugNameRule(d.Rule) || ErxComparisonEngine.IsNdcRule(d.Rule)))
                    {
                        resolved = true;
                        reason = "System (Matching NDC)";
                    }
                    else if (isQtyOrderedCompliant && ErxComparisonEngine.IsQuantityOrderedRule(d.Rule))
                    {
                        resolved = true;
                        reason = "System (Package Size / Unit Equivalence)";
                    }
                    else if (isQtyDispensedCompliant && ErxComparisonEngine.IsQuantityDispensedRule(d.Rule))
                    {
                        resolved = true;
                        reason = "System (Qty Dispensed <= Qty Ordered / Package Equivalence)";
                    }
                    else if (isDaysSupplyCompliant && ErxComparisonEngine.IsDaysSupplyRule(d.Rule))
                    {
                        resolved = true;
                        reason = "System (Sig Dosing / Multi-Dose Vial BUD Compliance)";
                    }

                    if (resolved)
                    {
                        d.Status = "Corrected";
                        d.ReviewedBy = reason;
                        d.ReviewedAt = DateTime.UtcNow;
                        discrepanciesToPersist.Add(d);
                    }
                }
            }

            if (discrepanciesToPersist.Count > 0)
            {
                try
                {
                    await _discrepancyRepository.BulkUpsertAsync(discrepanciesToPersist, "DiscrepancyId");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[AuditService:GetAllRecords] Failed to bulk update {Count} auto-resolved discrepancies: {Message}",
                        discrepanciesToPersist.Count, ex.Message);
                }
            }

            // 9. Map only the paged records to DTO
            var dtos = _mapper.Map<List<ErxAuditRecordDto>>(paged);
            foreach (var dto in dtos)
            {
                var rec = paged.FirstOrDefault(p => p.RecordId == dto.RecordId);
                var calcDays = ErxComparisonEngine.CalculateErxBasedDaysSupply(rec);
                dto.ErxBasedDaysSupply = calcDays ?? dto.ErxDaysSupply;
                if (calcDays.HasValue)
                {
                    dto.ErxDaysSupply = calcDays.Value;
                }
            }

            _logger.LogInformation("[AuditService:GetAllRecords] Total matching records: {TotalCount}, returning page of {Count} records.",
                totalCount, dtos.Count);

            return new PagedList<ErxAuditRecordDto>(dtos, pageParam ?? new PageParam(), totalCount);
        }

        public async Task<ErxAuditRecordDto?> GetRecordDiscrepanciesAsync(long recordId)
        {
            _logger.LogInformation("[AuditService:GetRecordDiscrepancies] Fetching discrepancies for RecordId: {RecordId}.", recordId);

            var includes = new Expression<Func<ErxAuditRecord, object>>[]
            {
                r => r.Discrepancies
            };

            var records = await _recordRepository.GetAllAsync(r => r.RecordId == recordId, includes);
            var record = records.FirstOrDefault();
            if (record == null)
            {
                _logger.LogWarning("[AuditService:GetRecordDiscrepancies] RecordId: {RecordId} not found.", recordId);
                return null;
            }

            var rules = (await _ruleRepository.GetAllAsync()).ToDictionary(r => r.RuleId);
            var hasMatchingDemographics = ErxComparisonEngine.ArePatientDemographicsMatching(record);
            var isControlled = _deaScheduleService.IsClass2Narcotic(record) || _deaScheduleService.IsClass3To5Narcotic(record);
            var hasMatchingNdc = ErxComparisonEngine.AreNdcsMatching(record) ||
                                 (_dbContext != null && PharmacyDrugNormalizer.AreNdcsEquivalent(record.MmDispensedNdc, record.ErxNdc, record, _dbContext));
            var isQtyOrderedCompliant = ErxComparisonEngine.IsQuantityOrderedCompliant(record);
            var isQtyDispensedCompliant = ErxComparisonEngine.IsQuantityDispensedCompliant(record);
            var isDaysSupplyCompliant = ErxComparisonEngine.IsDaysSupplyCompliant(record);

            foreach (var d in record.Discrepancies)
            {
                if (rules.TryGetValue(d.RuleId, out var rule))
                    d.Rule = rule;

                // If demographics match (DOB, Phone, Address), auto-resolve open Patient Name discrepancy
                if (hasMatchingDemographics && d.Rule != null && ErxComparisonEngine.IsPatientNameRule(d.Rule) &&
                    (d.Status == "Open" || string.IsNullOrEmpty(d.Status)))
                {
                    d.Status = "Corrected";
                    d.ReviewedBy = "System (Demographics Match)";
                    d.ReviewedAt = DateTime.UtcNow;
                    _ = _discrepancyRepository.UpsertAsync(d, "DiscrepancyId");
                }

                // For DEA, if the drug is non-controlled, auto-resolve open DEA discrepancy
                if (!isControlled && d.Rule != null && ErxComparisonEngine.IsDeaRule(d.Rule) &&
                    (d.Status == "Open" || string.IsNullOrEmpty(d.Status)))
                {
                    d.Status = "Corrected";
                    d.ReviewedBy = "System (Non-Controlled Drug)";
                    d.ReviewedAt = DateTime.UtcNow;
                    _ = _discrepancyRepository.UpsertAsync(d, "DiscrepancyId");
                }

                // For matching NDCs, auto-resolve Drug Name and NDC Code discrepancies
                if (hasMatchingNdc && d.Rule != null &&
                    (ErxComparisonEngine.IsDrugNameRule(d.Rule) || ErxComparisonEngine.IsNdcRule(d.Rule)) &&
                    (d.Status == "Open" || string.IsNullOrEmpty(d.Status)))
                {
                    d.Status = "Corrected";
                    d.ReviewedBy = "System (Matching NDC)";
                    d.ReviewedAt = DateTime.UtcNow;
                    _ = _discrepancyRepository.UpsertAsync(d, "DiscrepancyId");
                }

                // For Quantity Ordered package size / unit equivalence, auto-resolve open Quantity Ordered discrepancy
                if (isQtyOrderedCompliant && d.Rule != null && ErxComparisonEngine.IsQuantityOrderedRule(d.Rule) &&
                    (d.Status == "Open" || string.IsNullOrEmpty(d.Status)))
                {
                    d.Status = "Corrected";
                    d.ReviewedBy = "System (Package Size / Unit Equivalence)";
                    d.ReviewedAt = DateTime.UtcNow;
                    _ = _discrepancyRepository.UpsertAsync(d, "DiscrepancyId");
                }

                // For Quantity Dispensed <= Quantity Ordered / package equivalence, auto-resolve open Quantity Dispensed discrepancy
                if (isQtyDispensedCompliant && d.Rule != null && ErxComparisonEngine.IsQuantityDispensedRule(d.Rule) &&
                    (d.Status == "Open" || string.IsNullOrEmpty(d.Status)))
                {
                    d.Status = "Corrected";
                    d.ReviewedBy = "System (Qty Dispensed <= Qty Ordered / Package Equivalence)";
                    d.ReviewedAt = DateTime.UtcNow;
                    _ = _discrepancyRepository.UpsertAsync(d, "DiscrepancyId");
                }

                // For Days Supply compliant with SIG dosing calculation or multi-dose vial BUD equivalence, auto-resolve open Days Supply discrepancy
                if (isDaysSupplyCompliant && d.Rule != null && ErxComparisonEngine.IsDaysSupplyRule(d.Rule) &&
                    (d.Status == "Open" || string.IsNullOrEmpty(d.Status)))
                {
                    d.Status = "Corrected";
                    d.ReviewedBy = "System (Sig Dosing / Multi-Dose Vial BUD Compliance)";
                    d.ReviewedAt = DateTime.UtcNow;
                    _ = _discrepancyRepository.UpsertAsync(d, "DiscrepancyId");
                }
            }

            _logger.LogInformation("[AuditService:GetRecordDiscrepancies] RecordId: {RecordId} (RxNumber: '{RxNumber}') retrieved with {Count} discrepancies.",
                recordId, record.RxNumber, record.Discrepancies.Count);

            var dto = _mapper.Map<ErxAuditRecordDto>(record);
            if (dto != null)
            {
                var calcDays = ErxComparisonEngine.CalculateErxBasedDaysSupply(record);
                dto.ErxBasedDaysSupply = calcDays ?? dto.ErxDaysSupply;
                if (calcDays.HasValue)
                {
                    dto.ErxDaysSupply = calcDays.Value;
                }
            }
            return dto;
        }

        public async Task<SuccessResult<ErxAuditDiscrepancyDto>> UpdateDiscrepancyStatusAsync(long discrepancyId, string status, string? reviewedBy = null, string? erxValue = null, string? mmValue = null)
        {
            _logger.LogInformation("[AuditService:UpdateDiscrepancyStatus] Updating DiscrepancyId: {DiscrepancyId} to '{Status}' by '{ReviewedBy}' (MmVal: '{MmVal}', ErxVal: '{ErxVal}').",
                discrepancyId, status, reviewedBy, mmValue, erxValue);

            var discrepancy = await _discrepancyRepository.GetByIdAsync(discrepancyId, "DiscrepancyId");
            if (discrepancy == null)
            {
                _logger.LogWarning("[AuditService:UpdateDiscrepancyStatus] DiscrepancyId: {DiscrepancyId} not found.", discrepancyId);
                return SuccessResult<ErxAuditDiscrepancyDto>.NotFound("Discrepancy record not found.");
            }

            if (string.IsNullOrWhiteSpace(reviewedBy) && _tokenParser != null)
            {
                var tokenUser = _tokenParser.GetName();
                if (string.IsNullOrWhiteSpace(tokenUser))
                    tokenUser = _tokenParser.GetEmail();
                if (!string.IsNullOrWhiteSpace(tokenUser))
                    reviewedBy = tokenUser;
            }

            if (mmValue != null)
            {
                discrepancy.MmValue = mmValue.Trim();
                if (string.Equals(discrepancy.ErxValue?.Trim(), discrepancy.MmValue?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    status = "Corrected";
                }
            }

            if (erxValue != null)
            {
                discrepancy.ErxValue = erxValue.Trim();
                if (string.Equals(discrepancy.ErxValue?.Trim(), discrepancy.MmValue?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    status = "Corrected";
                }
            }

            discrepancy.Status = status;
            discrepancy.ReviewedBy = string.IsNullOrWhiteSpace(reviewedBy) ? "Admin" : reviewedBy.Trim();
            discrepancy.ReviewedAt = DateTime.UtcNow;

            await _discrepancyRepository.UpsertAsync(discrepancy, "DiscrepancyId");

            var record = await _recordRepository.GetByIdAsync(discrepancy.RecordId, "RecordId");
            if (record != null)
            {
                var rule = await _ruleRepository.GetByIdAsync(discrepancy.RuleId, "RuleId");
                if (rule != null)
                {
                    // Continuous Learning: Record resolution in Muse AI's permanent memory store
                    if (status.Equals("Corrected", StringComparison.OrdinalIgnoreCase) ||
                        status.Equals("Ignored", StringComparison.OrdinalIgnoreCase) ||
                        status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                    {
                        var mmValToLearn = discrepancy.MmValue ?? ErxComparisonEngine.GetPropertyValueAsString(record, rule.MmColumnName);
                        var erxValToLearn = discrepancy.ErxValue ?? ErxComparisonEngine.GetPropertyValueAsString(record, rule.ErxColumnName);
                        if (!string.IsNullOrWhiteSpace(mmValToLearn) && !string.IsNullOrWhiteSpace(erxValToLearn))
                        {
                            MuseClinicalMemoryStore.RecordResolutionMemory(
                                rule.FieldGroupName ?? "General",
                                mmValToLearn,
                                erxValToLearn,
                                status,
                                discrepancy.ReviewedBy,
                                record.MmDispensedDrugName ?? record.ErxDrugName);
                        }
                    }

                    var recordModified = false;
                    if (mmValue != null && !string.IsNullOrEmpty(rule.MmColumnName))
                    {
                        var col = rule.MmColumnName.Trim();
                        if (col.Equals("MmQtyDispensed", StringComparison.OrdinalIgnoreCase) && decimal.TryParse(mmValue, out var mqd)) { record.MmQtyDispensed = mqd; recordModified = true; }
                        else if (col.Equals("MmQtyOrd", StringComparison.OrdinalIgnoreCase) && decimal.TryParse(mmValue, out var mqo)) { record.MmQtyOrd = mqo; recordModified = true; }
                        else if (col.Equals("MmDispensedDaysSupply", StringComparison.OrdinalIgnoreCase) && int.TryParse(mmValue, out var mds)) { record.MmDispensedDaysSupply = mds; recordModified = true; }
                        else if (col.Equals("MmPatientName", StringComparison.OrdinalIgnoreCase)) { record.MmPatientName = mmValue; recordModified = true; }
                        else if (col.Equals("MmDispensedDrugName", StringComparison.OrdinalIgnoreCase)) { record.MmDispensedDrugName = mmValue; recordModified = true; }
                        else if (col.Equals("MmDispensedNdc", StringComparison.OrdinalIgnoreCase)) { record.MmDispensedNdc = mmValue; recordModified = true; }
                        else if (col.Equals("MmPrescriberName", StringComparison.OrdinalIgnoreCase)) { record.MmPrescriberName = mmValue; recordModified = true; }
                        else if (col.Equals("MmDispensedSig", StringComparison.OrdinalIgnoreCase)) { record.MmDispensedSig = mmValue; recordModified = true; }
                        else if (col.Equals("MmDawCode", StringComparison.OrdinalIgnoreCase)) { record.MmDawCode = mmValue; recordModified = true; }
                        else if (col.Equals("MmRefillsPharmacistEntered", StringComparison.OrdinalIgnoreCase) && int.TryParse(mmValue, out var mrf)) { record.MmRefillsPharmacistEntered = mrf; recordModified = true; }
                        else if (col.Equals("MmFillNumber", StringComparison.OrdinalIgnoreCase)) { record.MmFillNumber = mmValue; recordModified = true; }
                    }

                    if (erxValue != null && !string.IsNullOrEmpty(rule.ErxColumnName))
                    {
                        var col = rule.ErxColumnName.Trim();
                        if (col.Equals("ErxQtyDispensed", StringComparison.OrdinalIgnoreCase) && decimal.TryParse(erxValue, out var qd)) { record.ErxQtyDispensed = qd; recordModified = true; }
                        else if (col.Equals("ErxQtyOrd", StringComparison.OrdinalIgnoreCase) && decimal.TryParse(erxValue, out var qo)) { record.ErxQtyOrd = qo; recordModified = true; }
                        else if (col.Equals("ErxDaysSupply", StringComparison.OrdinalIgnoreCase) && int.TryParse(erxValue, out var ds)) { record.ErxDaysSupply = ds; recordModified = true; }
                        else if (col.Equals("ErxPatientName", StringComparison.OrdinalIgnoreCase)) { record.ErxPatientName = erxValue; recordModified = true; }
                        else if (col.Equals("ErxDrugName", StringComparison.OrdinalIgnoreCase)) { record.ErxDrugName = erxValue; recordModified = true; }
                        else if (col.Equals("ErxNdc", StringComparison.OrdinalIgnoreCase)) { record.ErxNdc = erxValue; recordModified = true; }
                        else if (col.Equals("ErxPrescriberName", StringComparison.OrdinalIgnoreCase)) { record.ErxPrescriberName = erxValue; recordModified = true; }
                        else if (col.Equals("ErxSig", StringComparison.OrdinalIgnoreCase)) { record.ErxSig = erxValue; recordModified = true; }
                        else if (col.Equals("ErxDawCode", StringComparison.OrdinalIgnoreCase)) { record.ErxDawCode = erxValue; recordModified = true; }
                        else if ((col.Equals("ErxRefill", StringComparison.OrdinalIgnoreCase) ||
                                  col.Equals("Refills Originally prescriber sent", StringComparison.OrdinalIgnoreCase) ||
                                  col.Replace(" ", "").Equals("RefillsOriginallyprescribersent", StringComparison.OrdinalIgnoreCase)) && int.TryParse(erxValue, out var rf)) { record.ErxRefill = rf; recordModified = true; }
                    }

                    if (recordModified)
                    {
                        await _recordRepository.UpsertAsync(record, "RecordId");
                    }
                }
            }

            var remainingRecordDiscrepancies = 0;
            var remainingBatchDiscrepancies = 0;
            var batchId = Guid.Empty;

            if (record != null)
            {
                batchId = record.BatchId;
                var recordDiscs = await _discrepancyRepository.GetAllAsync(d => d.RecordId == record.RecordId);
                remainingRecordDiscrepancies = recordDiscs.Count(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status));

                var batchRecords = (await _recordRepository.GetAllAsync(r => r.BatchId == record.BatchId)).ToList();
                var recordIds = batchRecords.Select(r => r.RecordId).ToHashSet();
                var batchDiscs = await _discrepancyRepository.GetAllAsync(d => recordIds.Contains(d.RecordId));
                remainingBatchDiscrepancies = batchDiscs.Count(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status));

                var batch = await _batchRepository.GetByIdAsync(record.BatchId, "BatchId");
                if (batch != null)
                {
                    batch.TotalDiscrepancies = remainingBatchDiscrepancies;
                    await _batchRepository.UpsertAsync(batch, "BatchId");
                }
            }

            var dto = _mapper.Map<ErxAuditDiscrepancyDto>(discrepancy);
            dto.BatchId = batchId;
            dto.RemainingBatchDiscrepancies = remainingBatchDiscrepancies;
            dto.RemainingRecordDiscrepancies = remainingRecordDiscrepancies;

            _logger.LogInformation("[AuditService:UpdateDiscrepancyStatus] DiscrepancyId: {DiscrepancyId} updated to '{Status}'. Remaining in Record: {RemRec}, Remaining in Batch: {RemBatch}.",
                discrepancyId, status, remainingRecordDiscrepancies, remainingBatchDiscrepancies);

            return SuccessResult<ErxAuditDiscrepancyDto>.Ok(dto, $"Discrepancy marked as {status}.");
        }

        public async Task<SuccessResult<BulkUpdateDiscrepancyResponseDto>> BulkUpdateDiscrepancyStatusAsync(BulkUpdateDiscrepancyStatusDto dto)
        {
            if (dto == null) return SuccessResult<BulkUpdateDiscrepancyResponseDto>.BadRequest("Request payload is required.");

            var status = string.IsNullOrWhiteSpace(dto.Status) ? "Ignored" : dto.Status.Trim();
            var reviewedBy = dto.ReviewedBy;
            if (string.IsNullOrWhiteSpace(reviewedBy) && _tokenParser != null)
            {
                var tokenUser = _tokenParser.GetName();
                if (string.IsNullOrWhiteSpace(tokenUser))
                    tokenUser = _tokenParser.GetEmail();
                if (!string.IsNullOrWhiteSpace(tokenUser))
                    reviewedBy = tokenUser;
            }
            if (string.IsNullOrWhiteSpace(reviewedBy)) reviewedBy = "Admin";

            _logger.LogInformation("[AuditService:BulkUpdateDiscrepancyStatus] Starting bulk update to '{Status}' by '{ReviewedBy}' (BatchId: {BatchId}, Column: '{Column}', RuleId: {RuleId}, DiscrepancyCount: {Count}).",
                status, reviewedBy, dto.BatchId, dto.DiscrepancyColumn, dto.RuleId, dto.DiscrepancyIds?.Count ?? 0);

            // Find candidate rules
            var rules = (await _ruleRepository.GetAllAsync()).ToList();
            var matchingRuleIds = new HashSet<int>();
            if (dto.RuleId.HasValue && dto.RuleId.Value > 0)
            {
                matchingRuleIds.Add(dto.RuleId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(dto.DiscrepancyColumn))
            {
                var col = dto.DiscrepancyColumn.Trim();
                foreach (var r in rules)
                {
                    if (r.FieldGroupName.Equals(col, StringComparison.OrdinalIgnoreCase) ||
                        r.MmColumnName.Equals(col, StringComparison.OrdinalIgnoreCase) ||
                        r.ErxColumnName.Equals(col, StringComparison.OrdinalIgnoreCase) ||
                        r.FieldGroupName.IndexOf(col, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matchingRuleIds.Add(r.RuleId);
                    }
                }
            }

            var includes = new Expression<Func<ErxAuditRecord, object>>[]
            {
                r => r.Discrepancies
            };

            Expression<Func<ErxAuditRecord, bool>> filter = r =>
                (!dto.BatchId.HasValue || r.BatchId == dto.BatchId.Value) &&
                (dto.RecordIds == null || !dto.RecordIds.Any() || dto.RecordIds.Contains(r.RecordId));

            var records = (await _recordRepository.GetAllAsync(filter, includes)).ToList();

            var targetDiscrepancies = new List<ErxAuditDiscrepancy>();
            var affectedRecordIds = new HashSet<long>();
            var affectedBatchIds = new HashSet<Guid>();

            foreach (var record in records)
            {
                foreach (var d in record.Discrepancies)
                {
                    bool isTarget = false;
                    if (dto.DiscrepancyIds != null && dto.DiscrepancyIds.Any())
                    {
                        if (dto.DiscrepancyIds.Contains(d.DiscrepancyId))
                            isTarget = true;
                    }
                    else if (matchingRuleIds.Any())
                    {
                        if (matchingRuleIds.Contains(d.RuleId))
                            isTarget = true;
                    }
                    else if (dto.RecordIds != null && dto.RecordIds.Any())
                    {
                        isTarget = true;
                    }

                    if (isTarget)
                    {
                        d.Status = status;
                        d.ReviewedBy = reviewedBy;
                        d.ReviewedAt = DateTime.UtcNow;
                        targetDiscrepancies.Add(d);
                        affectedRecordIds.Add(record.RecordId);
                        affectedBatchIds.Add(record.BatchId);

                        // Continuous Learning: Record bulk resolution in Muse AI's permanent memory store
                        if (status.Equals("Corrected", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Ignored", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                        {
                            var r = rules.FirstOrDefault(x => x.RuleId == d.RuleId);
                            if (r != null && !string.IsNullOrWhiteSpace(d.MmValue) && !string.IsNullOrWhiteSpace(d.ErxValue))
                            {
                                MuseClinicalMemoryStore.RecordResolutionMemory(
                                    r.FieldGroupName ?? "General",
                                    d.MmValue,
                                    d.ErxValue,
                                    status,
                                    reviewedBy,
                                    record.MmDispensedDrugName ?? record.ErxDrugName);
                            }
                        }
                    }
                }
            }

            if (targetDiscrepancies.Any())
            {
                await _discrepancyRepository.BulkUpsertAsync(targetDiscrepancies, "DiscrepancyId");
            }

            // Recompute total discrepancies for all affected batches
            int remainingInTargetBatch = 0;
            foreach (var bId in affectedBatchIds)
            {
                var bRecords = (await _recordRepository.GetAllAsync(r => r.BatchId == bId, includes)).ToList();
                var remainingBatchDiscs = bRecords.SelectMany(r => r.Discrepancies)
                    .Count(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status));

                var batch = await _batchRepository.GetByIdAsync(bId, "BatchId");
                if (batch != null)
                {
                    batch.TotalDiscrepancies = remainingBatchDiscs;
                    await _batchRepository.UpsertAsync(batch, "BatchId");
                }

                if (dto.BatchId.HasValue && bId == dto.BatchId.Value)
                {
                    remainingInTargetBatch = remainingBatchDiscs;
                }
                else if (!dto.BatchId.HasValue)
                {
                    remainingInTargetBatch += remainingBatchDiscs;
                }
            }

            var fieldMsg = !string.IsNullOrWhiteSpace(dto.DiscrepancyColumn) ? $" for '{dto.DiscrepancyColumn}'" : "";
            _logger.LogInformation("[AuditService:BulkUpdateDiscrepancyStatus] Bulk update complete. Updated {Count} discrepancies across {BatchCount} batches.",
                targetDiscrepancies.Count, affectedBatchIds.Count);

            return SuccessResult<BulkUpdateDiscrepancyResponseDto>.Ok(new BulkUpdateDiscrepancyResponseDto
            {
                UpdatedCount = targetDiscrepancies.Count,
                RemainingBatchDiscrepancies = remainingInTargetBatch,
                Message = $"Successfully marked {targetDiscrepancies.Count} discrepancy(ies){fieldMsg} as {status}.",
                AffectedRecordIds = affectedRecordIds.ToList()
            });
        }

        public async Task<List<ErxDiscrepancyColumnSummaryDto>> GetDiscrepancySummaryAsync(Guid? batchId = null, Guid? pharmacyId = null)
        {
            if (!pharmacyId.HasValue && _tokenParser != null && !_tokenParser.IsAdmin() && !_tokenParser.IsSuperAdmin())
            {
                pharmacyId = _tokenParser.GetPharmacyId();
            }

            _logger.LogInformation("[AuditService:GetDiscrepancySummary] Generating column discrepancy summary for BatchId: {BatchId}, PharmacyId: {PharmacyId}.", batchId, pharmacyId);

            // Automatically clean up any duplicate or test Patient Name rules and orphan discrepancies
            try
            {
                await CleanupDuplicatePatientNameRulesAsync(batchId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AuditService:GetDiscrepancySummary] Error running duplicate rules cleanup: {Message}", ex.Message);
            }

            // Auto-resolve any open Days Supply discrepancies that are clinically compliant (e.g. Multi-dose vial BUD 35 vs 70 days)
            try
            {
                await ResolveDaysSupplyCompliantDiscrepanciesAsync(batchId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AuditService:GetDiscrepancySummary] Error running Days Supply compliance auto-resolution: {Message}", ex.Message);
            }

            var discQuery = (_dbContext != null
                ? _dbContext.ErxAuditDiscrepancies.AsNoTracking()
                : _discrepancyRepository.GetAllQueryable().AsNoTracking())
                .Where(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status));

            if (batchId.HasValue && batchId.Value != Guid.Empty)
            {
                discQuery = discQuery.Where(d => d.Record != null && d.Record.BatchId == batchId.Value);
            }

            if (pharmacyId.HasValue && pharmacyId.Value != Guid.Empty)
            {
                discQuery = discQuery.Where(d => d.Record != null && d.Record.Batch != null && d.Record.Batch.PharmacyId == pharmacyId.Value);
            }

            var discItems = await discQuery
                .Select(d => new { d.RuleId, d.RecordId })
                .ToListAsync();

            var activeRules = (await _ruleRepository.GetAllAsync()).ToDictionary(r => r.RuleId);

            var summaries = discItems
                .GroupBy(x => x.RuleId)
                .Select(g =>
                {
                    activeRules.TryGetValue(g.Key, out var rule);
                    return new ErxDiscrepancyColumnSummaryDto
                    {
                        RuleId = g.Key,
                        FieldGroupName = rule?.FieldGroupName ?? "Unknown Rule",
                        MmColumnName = rule?.MmColumnName ?? "",
                        ErxColumnName = rule?.ErxColumnName ?? "",
                        DiscrepancyCount = g.Count(),
                        RecordCount = g.Select(x => x.RecordId).Distinct().Count()
                    };
                })
                .Where(s => !s.FieldGroupName.Contains("Testing", StringComparison.OrdinalIgnoreCase) &&
                            !s.FieldGroupName.Contains("Comparision", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.DiscrepancyCount)
                .ToList();

            if (batchId.HasValue && batchId.Value != Guid.Empty)
            {
                var totalRemaining = summaries.Sum(s => s.DiscrepancyCount);
                var batch = await _batchRepository.GetByIdAsync(batchId.Value, "BatchId");
                if (batch != null && batch.TotalDiscrepancies != totalRemaining)
                {
                    batch.TotalDiscrepancies = totalRemaining;
                    await _batchRepository.UpsertAsync(batch, "BatchId");
                }
            }

            _logger.LogInformation("[AuditService:GetDiscrepancySummary] Discrepancy summary calculated: {Count} column groups found.", summaries.Count);
            return summaries;
        }

        public async Task<ErxAuditFilterOptionsDto> GetFilterOptionsAsync(Guid? batchId = null, Guid? pharmacyId = null)
        {
            if (!pharmacyId.HasValue && _tokenParser != null && !_tokenParser.IsAdmin() && !_tokenParser.IsSuperAdmin())
            {
                pharmacyId = _tokenParser.GetPharmacyId();
            }

            _logger.LogInformation("[AuditService:GetFilterOptions] Loading filter options for BatchId: {BatchId}, PharmacyId: {PharmacyId}...", batchId, pharmacyId);

            IQueryable<ErxAuditRecord> query = _dbContext != null
                ? _dbContext.ErxAuditRecords.AsNoTracking()
                : _recordRepository.GetAllQueryable().AsNoTracking();

            if (batchId.HasValue && batchId.Value != Guid.Empty)
            {
                query = query.Where(r => r.BatchId == batchId.Value);
            }

            if (pharmacyId.HasValue && pharmacyId.Value != Guid.Empty)
            {
                query = query.Where(r => r.Batch != null && r.Batch.PharmacyId == pharmacyId.Value);
            }

            var totalRecords = await query.CountAsync();

            var paymentMethods = await query
                .Where(r => r.PaymentMethod != null && r.PaymentMethod.Trim() != "")
                .Select(r => r.PaymentMethod!.Trim())
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            var insCodes = await query
                .Where(r => r.InsCode != null && r.InsCode.Trim() != "")
                .Select(r => r.InsCode!.Trim())
                .Distinct()
                .OrderBy(i => i)
                .ToListAsync();

            var binNos = await query
                .Where(r => r.BinNo != null && r.BinNo.Trim() != "")
                .Select(r => r.BinNo!.Trim())
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            var rxOrigins = await query
                .Where(r => r.MmRxOrigin != null && r.MmRxOrigin.Trim() != "")
                .Select(r => r.MmRxOrigin!.Trim())
                .Distinct()
                .OrderBy(o => o)
                .ToListAsync();

            var insurancePriorityCount = await query.CountAsync(r =>
                r.PaymentMethod != null && (r.PaymentMethod == "I" || r.PaymentMethod == "Insurance"));

            var highValueClaimCount = await query.CountAsync(r =>
                r.TotalInsPaid.HasValue && r.TotalInsPaid.Value >= 500m);

            var nonElectronicCount = await query.CountAsync(r =>
                r.MmRxOrigin != null && r.MmRxOrigin.ToUpper() != "ELECTRONIC");

            return new ErxAuditFilterOptionsDto
            {
                TotalRecords = totalRecords,
                PaymentMethods = paymentMethods,
                InsCodes = insCodes,
                BinNos = binNos,
                RxOrigins = rxOrigins,
                InsurancePriorityCount = insurancePriorityCount,
                HighValueClaimCount = highValueClaimCount,
                NonElectronicCount = nonElectronicCount
            };
        }

        public async Task<PagedList<ErxColumnMatchRuleDto>> GetRulesAsync(PageParam pageParam, int? insuranceCompanyId = null)
        {
            await EnsureComplianceRulesSeededAsync();

            pageParam ??= new PageParam();

            Expression<Func<ErxColumnMatchRule, bool>>? filter = insuranceCompanyId.HasValue
                ? r => r.InsuranceCompanyId == insuranceCompanyId.Value || r.InsuranceCompanyId == null
                : null;

            var includes = new Expression<Func<ErxColumnMatchRule, object>>[]
            {
                r => r.InsuranceCompany!
            };

            var rules = (await _ruleRepository.GetAllAsync(filter, includes)).OrderByDescending(r => r.RuleId).ToList();
            var dtos = _mapper.Map<List<ErxColumnMatchRuleDto>>(rules);
            foreach (var dto in dtos)
            {
                var rule = rules.FirstOrDefault(r => r.RuleId == dto.RuleId);
                if (rule != null && !string.IsNullOrWhiteSpace(rule.InsuranceCompanyNames))
                {
                    dto.InsuranceCompanyName = rule.InsuranceCompanyNames;
                }
            }

            if (!string.IsNullOrWhiteSpace(pageParam.SearchText))
            {
                var q = pageParam.SearchText.Trim().ToLowerInvariant();
                dtos = dtos.Where(r =>
                    r.RuleId.ToString().Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(r.FieldGroupName) && r.FieldGroupName.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.MmColumnName) && r.MmColumnName.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.ErxColumnName) && r.ErxColumnName.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.MatchType) && r.MatchType.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.InsuranceCompanyName) && r.InsuranceCompanyName.ToLowerInvariant().Contains(q))
                ).ToList();
            }

            var totalRecords = dtos.Count;
            var page = pageParam.Page is > 0 ? pageParam.Page.Value : 1;
            var pageSize = pageParam.PageSize is > 0 ? pageParam.PageSize.Value : 10;
            var paged = dtos
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var pageParamForResult = new PageParam
            {
                Page = page,
                PageSize = pageSize,
                SearchText = pageParam.SearchText,
                SortBy = pageParam.SortBy,
                SortDirection = pageParam.SortDirection
            };

            _logger.LogInformation(
                "[AuditService:GetRules] Retrieved {Count} rules of {Total} (InsuranceCompanyId: {InsuranceCompanyId}, Page: {Page}, PageSize: {PageSize}).",
                paged.Count, totalRecords, insuranceCompanyId, page, pageSize);

            return new PagedList<ErxColumnMatchRuleDto>(paged, pageParamForResult, totalRecords);
        }

        private async Task EnsureComplianceRulesSeededAsync()
        {
            try
            {
                var existingRules = (await _ruleRepository.GetAllAsync()).ToList();

                var defaultRules = new List<ErxColumnMatchRule>
                {
                    new()
                    {
                        FieldGroupName = "Fill Date Sequence",
                        MmColumnName = "MmDateFilled",
                        ErxColumnName = "ErxOrderDate",
                        MatchType = "DateSameOrAfter",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    },
                    new()
                    {
                        FieldGroupName = "Effective Date Compliance",
                        MmColumnName = "MmDateFilled",
                        ErxColumnName = "ErxEffectiveDate",
                        MatchType = "EffectiveDateGoverned",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    },
                    new()
                    {
                        FieldGroupName = "Pickup Date Sequence",
                        MmColumnName = "PickupDate",
                        ErxColumnName = "MmDateFilled",
                        MatchType = "DateSameOrAfter",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    },
                    new()
                    {
                        FieldGroupName = "Class 2 Narcotic Refills",
                        MmColumnName = "MmFillNumber",
                        ErxColumnName = "ErxRefill",
                        MatchType = "Class2ZeroRefills",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    },
                    new()
                    {
                        FieldGroupName = "DEA Class 2 Narcotic 30-Day Expiration",
                        MmColumnName = "MmDateFilled",
                        ErxColumnName = "ErxEffectiveDate",
                        MatchType = "Class2Expiration30Days",
                        ToleranceValue = 30,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    },
                    new()
                    {
                        FieldGroupName = "DEA Class 3-5 Narcotic 180-Day Expiration",
                        MmColumnName = "MmDateFilled",
                        ErxColumnName = "ErxEffectiveDate",
                        MatchType = "Class3To5Expiration180Days",
                        ToleranceValue = 180,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    },
                    new()
                    {
                        FieldGroupName = "Texas Class 2 Narcotic 30-Day Expiration",
                        MmColumnName = "MmDateFilled",
                        ErxColumnName = "ErxEffectiveDate",
                        MatchType = "TexasClass2Expiration21Days",
                        ToleranceValue = 30,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    },
                    new()
                    {
                        FieldGroupName = "State-Specific Prescription Validity",
                        MmColumnName = "MmDateFilled",
                        ErxColumnName = "ErxOrderDate",
                        MatchType = "StateRxValidity",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    },
                    new()
                    {
                        FieldGroupName = "Supervising Physician Requirement (PA/NP)",
                        MmColumnName = "SupervisorName",
                        ErxColumnName = "SupervisorName",
                        MatchType = "SupervisingPhysicianRequired",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    }
                };

                foreach (var def in defaultRules)
                {
                    var match = existingRules.FirstOrDefault(r =>
                        r.FieldGroupName.Equals(def.FieldGroupName, StringComparison.OrdinalIgnoreCase) ||
                        (r.MmColumnName.Equals(def.MmColumnName, StringComparison.OrdinalIgnoreCase) &&
                         r.ErxColumnName.Equals(def.ErxColumnName, StringComparison.OrdinalIgnoreCase) &&
                         r.MatchType.Equals(def.MatchType, StringComparison.OrdinalIgnoreCase)));

                    if (match == null)
                    {
                        await _ruleRepository.UpsertAsync(def, "RuleId");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not auto-seed compliance audit rules: {Message}", ex.Message);
            }
        }

        public async Task<SuccessResult<ErxColumnMatchRuleDto>> CreateRuleAsync(ErxColumnMatchRuleDto dto)
        {
            if (dto == null) return SuccessResult<ErxColumnMatchRuleDto>.BadRequest("Rule data is required.");

            _logger.LogInformation("[AuditService:CreateRule] Creating rule for FieldGroup '{Group}', MmCol '{MmCol}', ErxCol '{ErxCol}', MatchType '{MatchType}'.",
                dto.FieldGroupName, dto.MmColumnName, dto.ErxColumnName, dto.MatchType);

            try
            {
                var rule = _mapper.Map<ErxColumnMatchRule>(dto);
                rule.RuleId = 0;

                // Handle multiple insurance companies
                if (dto.InsuranceCompanyIds != null && dto.InsuranceCompanyIds.Any())
                {
                    var validIds = dto.InsuranceCompanyIds.Where(id => id > 0).Distinct().ToList();
                    if (validIds.Any())
                    {
                        rule.InsuranceCompanyIds = string.Join(",", validIds);
                        rule.InsuranceCompanyId = validIds.First();
                        if (dto.InsuranceCompanyNames != null && dto.InsuranceCompanyNames.Any())
                        {
                            rule.InsuranceCompanyNames = string.Join(", ", dto.InsuranceCompanyNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());
                        }
                        else
                        {
                            var comps = (await _insuranceCompanyRepository.GetAllAsync(c => validIds.Contains(c.InsuranceCompanyId))).ToList();
                            rule.InsuranceCompanyNames = string.Join(", ", comps.Select(c => c.Name));
                        }
                    }
                    else
                    {
                        rule.InsuranceCompanyIds = null;
                        rule.InsuranceCompanyNames = null;
                        rule.InsuranceCompanyId = null;
                    }
                }
                else if (dto.InsuranceCompanyNames != null && dto.InsuranceCompanyNames.Any())
                {
                    var names = dto.InsuranceCompanyNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
                    var comps = (await _insuranceCompanyRepository.GetAllAsync(c => names.Contains(c.Name))).ToList();
                    rule.InsuranceCompanyIds = comps.Any() ? string.Join(",", comps.Select(c => c.InsuranceCompanyId)) : null;
                    rule.InsuranceCompanyId = comps.FirstOrDefault()?.InsuranceCompanyId;
                    rule.InsuranceCompanyNames = string.Join(", ", names);
                }
                else if (dto.InsuranceCompanyId.HasValue && dto.InsuranceCompanyId.Value > 0)
                {
                    rule.InsuranceCompanyId = dto.InsuranceCompanyId.Value;
                    rule.InsuranceCompanyIds = dto.InsuranceCompanyId.Value.ToString();
                    var comp = await _insuranceCompanyRepository.GetByIdAsync(dto.InsuranceCompanyId.Value, "InsuranceCompanyId");
                    rule.InsuranceCompanyNames = comp?.Name;
                }
                else
                {
                    rule.InsuranceCompanyIds = null;
                    rule.InsuranceCompanyNames = null;
                    rule.InsuranceCompanyId = null;
                }

                rule.CreatedDate = DateTime.UtcNow;

                await _ruleRepository.UpsertAsync(rule, "RuleId");
                if (rule.InsuranceCompanyId.HasValue)
                {
                    rule.InsuranceCompany = await _insuranceCompanyRepository.GetByIdAsync(rule.InsuranceCompanyId.Value, "InsuranceCompanyId");
                }
                var createdDto = _mapper.Map<ErxColumnMatchRuleDto>(rule);
                if (!string.IsNullOrWhiteSpace(rule.InsuranceCompanyNames))
                {
                    createdDto.InsuranceCompanyName = rule.InsuranceCompanyNames;
                }

                _logger.LogInformation("[AuditService:CreateRule] Rule {RuleId} created successfully for '{Group}'.", rule.RuleId, rule.FieldGroupName);
                return SuccessResult<ErxColumnMatchRuleDto>.Ok(createdDto, "Rule created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ERx column match rule: {Message}", ex.Message);
                return SuccessResult<ErxColumnMatchRuleDto>.BadRequest($"Failed to create rule: {ex.Message}");
            }
        }

        public async Task<SuccessResult<ErxColumnMatchRuleDto>> UpdateRuleAsync(int ruleId, ErxColumnMatchRuleDto dto)
        {
            if (dto == null) return SuccessResult<ErxColumnMatchRuleDto>.BadRequest("Rule data is required.");

            _logger.LogInformation("[AuditService:UpdateRule] Updating RuleId {RuleId} ('{Group}').", ruleId, dto.FieldGroupName);

            try
            {
                var rule = await _ruleRepository.GetByIdAsync(ruleId, "RuleId");
                if (rule == null)
                {
                    _logger.LogWarning("[AuditService:UpdateRule] RuleId {RuleId} not found.", ruleId);
                    return SuccessResult<ErxColumnMatchRuleDto>.NotFound("Rule not found.");
                }

                rule.FieldGroupName = dto.FieldGroupName;
                rule.MmColumnName = dto.MmColumnName;
                rule.ErxColumnName = dto.ErxColumnName;
                rule.MatchType = dto.MatchType;
                rule.ToleranceValue = dto.ToleranceValue;

                // Handle multiple insurance companies
                if (dto.InsuranceCompanyIds != null && dto.InsuranceCompanyIds.Any())
                {
                    var validIds = dto.InsuranceCompanyIds.Where(id => id > 0).Distinct().ToList();
                    if (validIds.Any())
                    {
                        rule.InsuranceCompanyIds = string.Join(",", validIds);
                        rule.InsuranceCompanyId = validIds.First();
                        if (dto.InsuranceCompanyNames != null && dto.InsuranceCompanyNames.Any())
                        {
                            rule.InsuranceCompanyNames = string.Join(", ", dto.InsuranceCompanyNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());
                        }
                        else
                        {
                            var comps = (await _insuranceCompanyRepository.GetAllAsync(c => validIds.Contains(c.InsuranceCompanyId))).ToList();
                            rule.InsuranceCompanyNames = string.Join(", ", comps.Select(c => c.Name));
                        }
                    }
                    else
                    {
                        rule.InsuranceCompanyIds = null;
                        rule.InsuranceCompanyNames = null;
                        rule.InsuranceCompanyId = null;
                    }
                }
                else if (dto.InsuranceCompanyNames != null && dto.InsuranceCompanyNames.Any())
                {
                    var names = dto.InsuranceCompanyNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
                    var comps = (await _insuranceCompanyRepository.GetAllAsync(c => names.Contains(c.Name))).ToList();
                    rule.InsuranceCompanyIds = comps.Any() ? string.Join(",", comps.Select(c => c.InsuranceCompanyId)) : null;
                    rule.InsuranceCompanyId = comps.FirstOrDefault()?.InsuranceCompanyId;
                    rule.InsuranceCompanyNames = string.Join(", ", names);
                }
                else if (dto.InsuranceCompanyId.HasValue && dto.InsuranceCompanyId.Value > 0)
                {
                    rule.InsuranceCompanyId = dto.InsuranceCompanyId.Value;
                    rule.InsuranceCompanyIds = dto.InsuranceCompanyId.Value.ToString();
                    var comp = await _insuranceCompanyRepository.GetByIdAsync(dto.InsuranceCompanyId.Value, "InsuranceCompanyId");
                    rule.InsuranceCompanyNames = comp?.Name;
                }
                else
                {
                    rule.InsuranceCompanyIds = null;
                    rule.InsuranceCompanyNames = null;
                    rule.InsuranceCompanyId = null;
                }

                rule.IsActive = dto.IsActive;

                await _ruleRepository.UpsertAsync(rule, "RuleId");
                if (rule.InsuranceCompanyId.HasValue)
                {
                    rule.InsuranceCompany = await _insuranceCompanyRepository.GetByIdAsync(rule.InsuranceCompanyId.Value, "InsuranceCompanyId");
                }
                var updatedDto = _mapper.Map<ErxColumnMatchRuleDto>(rule);
                if (!string.IsNullOrWhiteSpace(rule.InsuranceCompanyNames))
                {
                    updatedDto.InsuranceCompanyName = rule.InsuranceCompanyNames;
                }

                _logger.LogInformation("[AuditService:UpdateRule] RuleId {RuleId} updated successfully.", ruleId);
                return SuccessResult<ErxColumnMatchRuleDto>.Ok(updatedDto, "Rule updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ERx column match rule {RuleId}: {Message}", ruleId, ex.Message);
                return SuccessResult<ErxColumnMatchRuleDto>.BadRequest($"Failed to update rule: {ex.Message}");
            }
        }

        public async Task<SuccessResult<string>> DeleteRuleAsync(int ruleId)
        {
            try
            {
                _logger.LogInformation("[AuditService:DeleteRule] Deleting rule {RuleId}...", ruleId);
                var rule = await _ruleRepository.GetByIdAsync(ruleId, "RuleId");
                if (rule == null)
                {
                    _logger.LogWarning("[AuditService:DeleteRule] RuleId {RuleId} not found.", ruleId);
                    return SuccessResult<string>.NotFound("Rule not found.");
                }

                // Delete referencing discrepancies first to avoid FK constraint conflict
                if (_dbContext != null)
                {
                    var discs = await _dbContext.ErxAuditDiscrepancies.Where(d => d.RuleId == ruleId).ToListAsync();
                    if (discs.Any())
                    {
                        _dbContext.ErxAuditDiscrepancies.RemoveRange(discs);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                else
                {
                    var discs = await _discrepancyRepository.GetAllAsync(d => d.RuleId == ruleId);
                    foreach (var d in discs)
                    {
                        await _discrepancyRepository.DeleteAsync(d.DiscrepancyId);
                    }
                }

                await _ruleRepository.DeleteAsync(ruleId);
                _logger.LogInformation("[AuditService:DeleteRule] RuleId {RuleId} ('{Group}') deleted successfully.", ruleId, rule.FieldGroupName);
                return SuccessResult<string>.Ok(rule.FieldGroupName, "Rule deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ERx column match rule {RuleId}: {Message}", ruleId, ex.Message);
                return SuccessResult<string>.BadRequest($"Failed to delete rule: {ex.Message}");
            }
        }

        public async Task<IEnumerable<InsuranceCompanyDto>> GetInsuranceCompaniesAsync()
        {
            try
            {
                _logger.LogInformation("[AuditService:GetInsuranceCompanies] Loading insurance companies for audit rules.");
                // Retrieve all insurance companies from the Insurance panel table (both active and inactive)
                var insurances = (await _insuranceRepository.GetAllAsync()).ToList();
                var existingCompanies = (await _insuranceCompanyRepository.GetAllAsync()).ToList();

                // Map BIN numbers from Insurance table
                var binMap = insurances
                    .Where(i => !string.IsNullOrWhiteSpace(i.InsuranceName))
                    .GroupBy(i => i.InsuranceName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.BINNumber, StringComparer.OrdinalIgnoreCase);

                // Synchronize any insurance companies from the Insurance panel into InsuranceCompany
                foreach (var ins in insurances)
                {
                    if (string.IsNullOrWhiteSpace(ins.InsuranceName)) continue;
                    var trimmedName = ins.InsuranceName.Trim();

                    var match = existingCompanies.FirstOrDefault(c =>
                        string.Equals(c.Name, trimmedName, StringComparison.OrdinalIgnoreCase));

                    if (match == null)
                    {
                        var newCompany = new InsuranceCompany
                        {
                            Name = trimmedName,
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        };
                        await _insuranceCompanyRepository.AddAsync(newCompany);
                        existingCompanies.Add(newCompany);
                    }
                }

                _logger.LogInformation("[AuditService:GetInsuranceCompanies] Retrieved {Count} insurance companies.", existingCompanies.Count);
                // Return all companies (both active and inactive) ordered by Name
                return existingCompanies
                    .OrderBy(c => c.Name)
                    .Select(c => new InsuranceCompanyDto
                    {
                        InsuranceCompanyId = c.InsuranceCompanyId,
                        Name = c.Name,
                        IsActive = c.IsActive,
                        BINNumber = binMap.TryGetValue(c.Name, out var bin) ? bin : null
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load insurance companies: {Message}", ex.Message);
                var fallback = (await _insuranceCompanyRepository.GetAllAsync()).ToList();
                return fallback
                    .OrderBy(c => c.Name)
                    .Select(c => new InsuranceCompanyDto
                    {
                        InsuranceCompanyId = c.InsuranceCompanyId,
                        Name = c.Name,
                        IsActive = c.IsActive
                    })
                    .ToList();
            }
        }

        public async Task<byte[]> ExportRecordsAsync(Guid? batchId, bool? discrepanciesOnly = null, string format = "xlsx", Guid? pharmacyId = null)
        {
            if (!pharmacyId.HasValue && _tokenParser != null && !_tokenParser.IsAdmin() && !_tokenParser.IsSuperAdmin())
            {
                pharmacyId = _tokenParser.GetPharmacyId();
            }

            _logger.LogInformation("[AuditService:ExportRecords] Starting export (BatchId: {BatchId}, PharmacyId: {PharmacyId}, DiscrepanciesOnly: {DiscrepanciesOnly}, Format: '{Format}').",
                batchId, pharmacyId, discrepanciesOnly, format);

            IQueryable<ErxAuditRecord> query = (_dbContext != null
                ? _dbContext.ErxAuditRecords.AsNoTracking()
                : _recordRepository.GetAllQueryable().AsNoTracking())
                .Include(r => r.Batch)
                    .ThenInclude(b => b.Pharmacy)
                .Include(r => r.Discrepancies)
                    .ThenInclude(d => d.Rule);

            if (batchId.HasValue && batchId.Value != Guid.Empty)
            {
                query = query.Where(r => r.BatchId == batchId.Value);
            }

            if (pharmacyId.HasValue && pharmacyId.Value != Guid.Empty)
            {
                query = query.Where(r => r.Batch != null && r.Batch.PharmacyId == pharmacyId.Value);
            }

            if (discrepanciesOnly == true)
            {
                query = query.Where(r => r.Discrepancies.Any(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status)));
            }

            var recordList = await query.OrderBy(r => r.RecordId).ToListAsync();

            byte[] exportBytes;
            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                exportBytes = GenerateCsv(recordList);
            }
            else
            {
                exportBytes = GenerateExcel(recordList);
            }

            _logger.LogInformation("[AuditService:ExportRecords] Export generated successfully: {Count} records exported ({Bytes} bytes).",
                recordList.Count, exportBytes.Length);

            return exportBytes;
        }

        private static readonly string[] ExportHeaders =
        [
            "Record ID",
            "Batch ID",
            "Pharmacy Name",
            "Pharmacy NPI",
            "RX Number",
            "MM Fill Number",
            "ERX RX Number",
            "DEA Schedule",
            "MM Order Date",
            "ERX Order Date",
            "ERX Effective Date",
            "MM Date Filled",
            "MM Time Filled",
            "Pickup Date",
            "ERX Received Date/Time",
            "Payment Method",
            "Insurance Code",
            "Insurance BIN",
            "Total Amount Paid",
            "Audit Priority",
            "High Value Claim",
            "Hardcopy Audit Flag",
            "MM Patient Name",
            "ERX Patient Name",
            "MM Patient DOB",
            "ERX Patient DOB",
            "MM Patient Phone",
            "ERX Patient Phone",
            "MM Patient Address",
            "ERX Patient Address",
            "MM Prescriber Name",
            "ERX Prescriber Name",
            "Supervisor Name",
            "MM Prescriber NPI",
            "ERX Prescriber NPI",
            "MM Prescriber DEA",
            "ERX Prescriber DEA",
            "MM Dispensed Drug",
            "ERX Drug Name",
            "MM Potency Unit",
            "ERX Potency Unit",
            "MM Dispensed NDC",
            "ERX NDC",
            "MM Qty Ord",
            "ERX Qty Ord",
            "MM Qty Dispensed",
            "ERX Qty Dispensed",
            "MM Days Supply",
            "ERX Days Supply",
            "MM Dispensed Sig",
            "ERX Sig",
            "MM DAW Code",
            "ERX DAW Code",
            "MM Refills (Pharmacist Entered)",
            "ERX Refills",
            "MM Copay",
            "MM Delivery",
            "MM Rx Origin",
            "MM Signature Captured",
            "Follow Up Tag",
            "MM RPh Notes",
            "Telephone Notes",
            "Telephone Notes Date",
            "ERX Notes",
            "ERX Notes Date",
            "ERX Notes Author",
            "DUR Reason",
            "DUR Prof",
            "DUR Result",
            "Message",
            "Audit Status",
            "Total Discrepancies",
            "Open Discrepancies",
            "Discrepancy Details"
        ];

        private string[] MapRecordToExportRow(ErxAuditRecord r)
        {
            var deaSchedule = _deaScheduleService.GetDeaSchedule(r);
            var deaLabel = _deaScheduleService.GetScheduleLabel(deaSchedule);

            var totalDisc = r.Discrepancies?.Count ?? 0;
            var openDisc = r.Discrepancies?.Count(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status)) ?? 0;

            string auditStatus;
            if (totalDisc == 0)
                auditStatus = "Clean";
            else if (openDisc > 0)
                auditStatus = "Discrepancy Detected";
            else
                auditStatus = "Resolved";

            var discrepancySummary = r.Discrepancies == null || r.Discrepancies.Count == 0
                ? "None"
                : string.Join("; ", r.Discrepancies.Select(d =>
                {
                    var ruleName = d.Rule?.FieldGroupName ?? $"Rule #{d.RuleId}";
                    return $"{ruleName}: MM='{d.MmValue}' vs ERX='{d.ErxValue}' [Status: {d.Status ?? "Open"}]";
                }));

            var pharmacyName = r.Batch?.Pharmacy != null
                ? (r.Batch.Pharmacy.BusinessTradeName ?? r.Batch.Pharmacy.BusinessLegalName ?? "")
                : "";
            var pharmacyNpi = r.Batch?.Pharmacy?.NPI ?? "";

            return
            [
                r.RecordId.ToString(),
                r.BatchId.ToString(),
                pharmacyName,
                pharmacyNpi,
                r.RxNumber ?? "",
                r.MmFillNumber ?? "",
                r.ErxRxNumber ?? "",
                deaLabel,
                r.MmOrderDate?.ToString("MM/dd/yyyy") ?? "",
                r.ErxOrderDate?.ToString("MM/dd/yyyy") ?? "",
                r.ErxEffectiveDate?.ToString("MM/dd/yyyy") ?? "",
                r.MmDateFilled?.ToString("MM/dd/yyyy") ?? "",
                r.MmTimeFilled?.ToString(@"hh\:mm") ?? "",
                r.PickupDate?.ToString("MM/dd/yyyy") ?? "",
                r.ErxReceivedDateTime?.ToString("MM/dd/yyyy HH:mm:ss") ?? "",
                r.PaymentMethod ?? "",
                r.InsCode ?? "",
                r.BinNo ?? "",
                r.TotalInsPaid?.ToString("0.00") ?? "",
                r.PaymentMethod == "I" || r.PaymentMethod == "Insurance" ? "Insurance Priority" : "Standard",
                (r.TotalInsPaid ?? 0) >= 500m ? "High Value (≥$500)" : "Normal",
                r.MmRxOrigin != null && !r.MmRxOrigin.Equals("ELECTRONIC", StringComparison.OrdinalIgnoreCase) ? "Non-Electronic (Hardcopy Review Required)" : "Electronic",
                r.MmPatientName ?? "",
                r.ErxPatientName ?? "",
                r.MmPatientDob?.ToString("MM/dd/yyyy") ?? "",
                r.ErxPatientDob?.ToString("MM/dd/yyyy") ?? "",
                r.MmPatientPhone ?? "",
                r.ErxPatientPhone ?? "",
                r.MmPatientAddress ?? "",
                r.ErxPatientAddress ?? "",
                r.MmPrescriberName ?? "",
                r.ErxPrescriberName ?? "",
                r.SupervisorName ?? "",
                r.MmPrescriberNpi ?? "",
                r.ErxPrescriberNpi ?? "",
                r.MmPrescriberDea ?? "",
                r.ErxPrescriberDea ?? "",
                r.MmDispensedDrugName ?? "",
                r.ErxDrugName ?? "",
                r.MmPotencyUnitCode ?? "",
                r.ErxPotencyUnitCode ?? "",
                r.MmDispensedNdc ?? "",
                r.ErxNdc ?? "",
                r.MmQtyOrd?.ToString("0.##") ?? "",
                r.ErxQtyOrd?.ToString("0.##") ?? "",
                r.MmQtyDispensed?.ToString("0.##") ?? "",
                r.ErxQtyDispensed?.ToString("0.##") ?? "",
                r.MmDispensedDaysSupply?.ToString() ?? "",
                r.ErxDaysSupply?.ToString() ?? "",
                r.MmDispensedSig ?? "",
                r.ErxSig ?? "",
                r.MmDawCode ?? "",
                r.ErxDawCode ?? "",
                r.MmRefillsPharmacistEntered?.ToString() ?? "",
                r.ErxRefill?.ToString() ?? "",
                r.MmCopay?.ToString("0.00") ?? "",
                r.MmDelivery ?? "",
                r.MmRxOrigin ?? "",
                r.MmSignatureCaptured.HasValue ? (r.MmSignatureCaptured.Value ? "Yes" : "No") : "",
                r.FollowUpTag ?? "",
                r.MmRphNotes ?? "",
                r.TelephoneNotes ?? "",
                r.TelephoneNotesDate?.ToString("MM/dd/yyyy") ?? "",
                r.ErxNotes ?? "",
                r.ErxNotesDate?.ToString("MM/dd/yyyy") ?? "",
                r.ErxNotesCreatedBy ?? "",
                r.DurReasonCd ?? "",
                r.DurProfCd ?? "",
                r.DurResultCode ?? "",
                r.Message ?? "",
                auditStatus,
                totalDisc.ToString(),
                openDisc.ToString(),
                discrepancySummary
            ];
        }

        private byte[] GenerateCsv(List<ErxAuditRecord> records)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", ExportHeaders.Select(EscapeCsvValue)));

            foreach (var record in records)
            {
                var row = MapRecordToExportRow(record);
                sb.AppendLine(string.Join(",", row.Select(EscapeCsvValue)));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private byte[] GenerateExcel(List<ErxAuditRecord> records)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("ERx Audit Records");

            // Header row
            for (var col = 0; col < ExportHeaders.Length; col++)
            {
                var cell = worksheet.Cell(1, col + 1);
                cell.Value = ExportHeaders[col];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(30, 58, 138); // Navy blue
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            worksheet.Row(1).Height = 26;

            // Data rows
            var rowIndex = 2;
            foreach (var record in records)
            {
                var row = MapRecordToExportRow(record);
                for (var col = 0; col < row.Length; col++)
                {
                    worksheet.Cell(rowIndex, col + 1).Value = row[col];
                }

                if (rowIndex % 2 == 1)
                {
                    worksheet.Row(rowIndex).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252);
                }

                rowIndex++;
            }

            worksheet.Columns().AdjustToContents(10.0, 50.0);
            worksheet.ShowGridLines = true;

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public async Task<int> ResolvePatientNameMatchingDemographicsAsync(Guid? batchId = null)
        {
            _logger.LogInformation("[AuditService:ResolveMatchingDemographics] Checking BatchId: {BatchId} for matching demographics (DOB, Phone, Address)...", batchId);

            var includes = new Expression<Func<ErxAuditRecord, object>>[]
            {
                r => r.Discrepancies
            };

            Expression<Func<ErxAuditRecord, bool>> filter = r => !batchId.HasValue || r.BatchId == batchId.Value;
            var records = (await _recordRepository.GetAllAsync(filter, includes)).ToList();
            var rules = (await _ruleRepository.GetAllAsync()).ToDictionary(r => r.RuleId);

            var resolvedCount = 0;
            var affectedBatchIds = new HashSet<Guid>();

            foreach (var record in records)
            {
                if (!ErxComparisonEngine.ArePatientDemographicsMatching(record))
                    continue;

                foreach (var d in record.Discrepancies)
                {
                    if (rules.TryGetValue(d.RuleId, out var rule))
                        d.Rule = rule;

                    if (d.Rule != null && ErxComparisonEngine.IsPatientNameRule(d.Rule) &&
                        (d.Status == "Open" || string.IsNullOrEmpty(d.Status)))
                    {
                        d.Status = "Corrected";
                        d.ReviewedBy = "System (Demographics Match)";
                        d.ReviewedAt = DateTime.UtcNow;
                        await _discrepancyRepository.UpsertAsync(d, "DiscrepancyId");
                        resolvedCount++;
                        affectedBatchIds.Add(record.BatchId);
                    }
                }
            }

            foreach (var bId in affectedBatchIds)
            {
                var batch = await _batchRepository.GetByIdAsync(bId, "BatchId");
                if (batch != null)
                {
                    var bRecords = (await _recordRepository.GetAllAsync(r => r.BatchId == bId)).ToList();
                    var recordIds = bRecords.Select(r => r.RecordId).ToHashSet();
                    var bDiscs = await _discrepancyRepository.GetAllAsync(d => recordIds.Contains(d.RecordId));
                    batch.TotalDiscrepancies = bDiscs.Count(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status));
                    await _batchRepository.UpsertAsync(batch, "BatchId");
                }
            }

            _logger.LogInformation("[AuditService:ResolveMatchingDemographics] Auto-resolved {Count} patient name discrepancies across {BatchCount} batches.",
                resolvedCount, affectedBatchIds.Count);

            return resolvedCount;
        }

        public async Task<int> ResolveDaysSupplyCompliantDiscrepanciesAsync(Guid? batchId = null)
        {
            _logger.LogInformation("[AuditService:ResolveDaysSupplyCompliant] Checking BatchId: {BatchId} for compliant Days Supply discrepancies...", batchId);

            var includes = new Expression<Func<ErxAuditRecord, object>>[]
            {
                r => r.Discrepancies
            };

            Expression<Func<ErxAuditRecord, bool>> filter = r => !batchId.HasValue || r.BatchId == batchId.Value;
            var records = (await _recordRepository.GetAllAsync(filter, includes)).ToList();
            var rules = (await _ruleRepository.GetAllAsync()).ToDictionary(r => r.RuleId);

            var resolvedCount = 0;
            var affectedBatchIds = new HashSet<Guid>();

            foreach (var record in records)
            {
                if (!ErxComparisonEngine.IsDaysSupplyCompliant(record))
                    continue;

                foreach (var d in record.Discrepancies)
                {
                    if (rules.TryGetValue(d.RuleId, out var rule))
                        d.Rule = rule;

                    if (d.Rule != null && ErxComparisonEngine.IsDaysSupplyRule(d.Rule) &&
                        (d.Status == "Open" || string.IsNullOrEmpty(d.Status)))
                    {
                        d.Status = "Corrected";
                        d.ReviewedBy = "System (Sig Dosing / Multi-Dose Vial BUD Compliance)";
                        d.ReviewedAt = DateTime.UtcNow;
                        await _discrepancyRepository.UpsertAsync(d, "DiscrepancyId");
                        resolvedCount++;
                        affectedBatchIds.Add(record.BatchId);
                    }
                }
            }

            if (resolvedCount > 0)
            {
                foreach (var bId in affectedBatchIds)
                {
                    var batch = await _batchRepository.GetByIdAsync(bId, "BatchId");
                    if (batch != null)
                    {
                        var bRecords = (await _recordRepository.GetAllAsync(r => r.BatchId == bId)).ToList();
                        var recordIds = bRecords.Select(r => r.RecordId).ToHashSet();
                        var bDiscs = await _discrepancyRepository.GetAllAsync(d => recordIds.Contains(d.RecordId));
                        batch.TotalDiscrepancies = bDiscs.Count(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status));
                        await _batchRepository.UpsertAsync(batch, "BatchId");
                    }
                }
            }

            _logger.LogInformation("[AuditService:ResolveDaysSupplyCompliant] Auto-resolved {Count} Days Supply discrepancies across {BatchCount} batches.",
                resolvedCount, affectedBatchIds.Count);

            return resolvedCount;
        }

        public async Task<int> CleanupDuplicatePatientNameRulesAsync(Guid? batchId = null)
        {
            _logger.LogInformation("[AuditService:CleanupDuplicateRules] Checking for duplicate Patient Name rules to cleanup (BatchId: {BatchId})...", batchId);

            try
            {
                var allRules = (await _ruleRepository.GetAllAsync()).ToList();

                // Canonical rule: the first active "Patient Name" rule with the lowest RuleId
                var canonicalPatientRule = allRules
                    .Where(r => r.FieldGroupName.Equals("Patient Name", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(r => r.RuleId)
                    .FirstOrDefault();

                // Find duplicate or test rules:
                // 1) Any rule with "Comparision" or "Testing" in its FieldGroupName
                // 2) Any additional Patient Name rule whose RuleId != canonicalPatientRule.RuleId
                var duplicateRules = allRules.Where(r =>
                    r.FieldGroupName.IndexOf("Comparision", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.FieldGroupName.IndexOf("Testing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (canonicalPatientRule != null && r.RuleId != canonicalPatientRule.RuleId &&
                     (r.FieldGroupName.Equals("Patient Name", StringComparison.OrdinalIgnoreCase) ||
                      (r.MmColumnName.Equals("MmPatientName", StringComparison.OrdinalIgnoreCase) &&
                       r.ErxColumnName.Equals("ErxPatientName", StringComparison.OrdinalIgnoreCase))))
                ).ToList();

                if (!duplicateRules.Any())
                {
                    _logger.LogInformation("[AuditService:CleanupDuplicateRules] No duplicate patient name rules found.");
                    return 0;
                }

                var duplicateRuleIds = duplicateRules.Select(r => r.RuleId).ToHashSet();
                _logger.LogInformation("[AuditService:CleanupDuplicateRules] Identified {Count} duplicate rules to purge: {Names}",
                    duplicateRules.Count, string.Join(", ", duplicateRules.Select(r => $"{r.FieldGroupName} (ID: {r.RuleId})")));

                var affectedBatchIds = new HashSet<Guid>();
                if (batchId.HasValue && batchId.Value != Guid.Empty)
                {
                    affectedBatchIds.Add(batchId.Value);
                }

                // Delete associated discrepancies first to satisfy FK constraint
                if (_dbContext != null)
                {
                    var discsToDelete = await _dbContext.ErxAuditDiscrepancies
                        .Where(d => duplicateRuleIds.Contains(d.RuleId))
                        .ToListAsync();

                    if (discsToDelete.Any())
                    {
                        var recIds = discsToDelete.Select(d => d.RecordId).Distinct().ToList();
                        var batchIds = await _dbContext.ErxAuditRecords
                            .Where(r => recIds.Contains(r.RecordId))
                            .Select(r => r.BatchId)
                            .Distinct()
                            .ToListAsync();

                        foreach (var b in batchIds) affectedBatchIds.Add(b);

                        _dbContext.ErxAuditDiscrepancies.RemoveRange(discsToDelete);
                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("[AuditService:CleanupDuplicateRules] Deleted {Count} orphan discrepancies referencing duplicate rules.", discsToDelete.Count);
                    }

                    var rulesToDelete = await _dbContext.ErxColumnMatchRules
                        .Where(r => duplicateRuleIds.Contains(r.RuleId))
                        .ToListAsync();

                    if (rulesToDelete.Any())
                    {
                        _dbContext.ErxColumnMatchRules.RemoveRange(rulesToDelete);
                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("[AuditService:CleanupDuplicateRules] Deleted {Count} duplicate rules from database.", rulesToDelete.Count);
                    }
                }
                else
                {
                    var discsToDelete = await _discrepancyRepository.GetAllAsync(d => duplicateRuleIds.Contains(d.RuleId));
                    if (discsToDelete != null && discsToDelete.Any())
                    {
                        var recIds = discsToDelete.Select(d => d.RecordId).Distinct().ToList();
                        var records = await _recordRepository.GetAllAsync(r => recIds.Contains(r.RecordId));
                        foreach (var rec in records) affectedBatchIds.Add(rec.BatchId);

                        foreach (var disc in discsToDelete)
                        {
                            await _discrepancyRepository.DeleteAsync(disc.DiscrepancyId);
                        }
                    }

                    foreach (var rule in duplicateRules)
                    {
                        await _ruleRepository.DeleteAsync(rule.RuleId);
                    }
                }

                // Recalculate batch TotalDiscrepancies for all affected batches
                foreach (var bId in affectedBatchIds)
                {
                    var batch = await _batchRepository.GetByIdAsync(bId, "BatchId");
                    if (batch != null)
                    {
                        var bRecords = (await _recordRepository.GetAllAsync(r => r.BatchId == bId)).ToList();
                        var bRecordIds = bRecords.Select(r => r.RecordId).ToHashSet();
                        var bDiscs = await _discrepancyRepository.GetAllAsync(d => bRecordIds.Contains(d.RecordId));
                        batch.TotalDiscrepancies = bDiscs.Count(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status));
                        await _batchRepository.UpsertAsync(batch, "BatchId");
                        _logger.LogInformation("[AuditService:CleanupDuplicateRules] Recalculated Batch {BatchId} TotalDiscrepancies to {Total}.",
                            bId, batch.TotalDiscrepancies);
                    }
                }

                return duplicateRules.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuditService:CleanupDuplicateRules] Error cleaning up duplicate patient name rules: {Message}", ex.Message);
                return 0;
            }
        }

        public async Task<SuccessResult<ErxAuditRecordDto>> UpdateRecordAsync(long recordId, UpdateErxAuditRecordDto dto)
        {
            _logger.LogInformation("[AuditService:UpdateRecord] Updating RecordId: {RecordId} (RxNumber: '{RxNumber}')...", recordId, dto.RxNumber);

            var record = await _recordRepository.GetByIdAsync(recordId, "RecordId");
            if (record == null)
            {
                _logger.LogWarning("[AuditService:UpdateRecord] RecordId: {RecordId} not found.", recordId);
                return SuccessResult<ErxAuditRecordDto>.NotFound("Record not found.");
            }

            // Update MM / ERX fields
            if (dto.RxNumber != null) record.RxNumber = dto.RxNumber;
            if (dto.MmFillNumber != null) record.MmFillNumber = dto.MmFillNumber;
            if (dto.ErxRxNumber != null) record.ErxRxNumber = dto.ErxRxNumber;

            if (dto.MmOrderDate.HasValue) record.MmOrderDate = dto.MmOrderDate.Value;
            if (dto.ErxOrderDate.HasValue) record.ErxOrderDate = dto.ErxOrderDate.Value;
            if (dto.ErxEffectiveDate.HasValue) record.ErxEffectiveDate = dto.ErxEffectiveDate.Value;
            if (dto.MmDateFilled.HasValue) record.MmDateFilled = dto.MmDateFilled.Value;
            if (dto.MmTimeFilled.HasValue) record.MmTimeFilled = dto.MmTimeFilled.Value;
            if (dto.PickupDate.HasValue) record.PickupDate = dto.PickupDate.Value;
            if (dto.ErxReceivedDateTime.HasValue) record.ErxReceivedDateTime = dto.ErxReceivedDateTime.Value;

            if (dto.MmPatientName != null) record.MmPatientName = dto.MmPatientName;
            if (dto.ErxPatientName != null) record.ErxPatientName = dto.ErxPatientName;

            if (dto.MmPatientDob.HasValue) record.MmPatientDob = dto.MmPatientDob.Value;
            if (dto.ErxPatientDob.HasValue) record.ErxPatientDob = dto.ErxPatientDob.Value;

            if (dto.MmPatientPhone != null) record.MmPatientPhone = dto.MmPatientPhone;
            if (dto.ErxPatientPhone != null) record.ErxPatientPhone = dto.ErxPatientPhone;

            if (dto.MmPatientAddress != null) record.MmPatientAddress = dto.MmPatientAddress;
            if (dto.ErxPatientAddress != null) record.ErxPatientAddress = dto.ErxPatientAddress;

            if (dto.MmPrescriberName != null) record.MmPrescriberName = dto.MmPrescriberName;
            if (dto.ErxPrescriberName != null) record.ErxPrescriberName = dto.ErxPrescriberName;
            if (dto.SupervisorName != null) record.SupervisorName = dto.SupervisorName;

            if (dto.MmPrescriberNpi != null) record.MmPrescriberNpi = dto.MmPrescriberNpi;
            if (dto.ErxPrescriberNpi != null) record.ErxPrescriberNpi = dto.ErxPrescriberNpi;

            if (dto.MmPrescriberDea != null) record.MmPrescriberDea = dto.MmPrescriberDea;
            if (dto.ErxPrescriberDea != null) record.ErxPrescriberDea = dto.ErxPrescriberDea;

            if (dto.MmDispensedDrugName != null) record.MmDispensedDrugName = dto.MmDispensedDrugName;
            if (dto.ErxDrugName != null) record.ErxDrugName = dto.ErxDrugName;

            if (dto.MmPotencyUnitCode != null) record.MmPotencyUnitCode = dto.MmPotencyUnitCode;
            if (dto.ErxPotencyUnitCode != null) record.ErxPotencyUnitCode = dto.ErxPotencyUnitCode;

            if (dto.MmDispensedNdc != null) record.MmDispensedNdc = dto.MmDispensedNdc;
            if (dto.ErxNdc != null) record.ErxNdc = dto.ErxNdc;

            if (dto.MmQtyOrd.HasValue) record.MmQtyOrd = dto.MmQtyOrd.Value;
            if (dto.ErxQtyOrd.HasValue) record.ErxQtyOrd = dto.ErxQtyOrd.Value;

            if (dto.MmQtyDispensed.HasValue) record.MmQtyDispensed = dto.MmQtyDispensed.Value;
            if (dto.ErxQtyDispensed.HasValue) record.ErxQtyDispensed = dto.ErxQtyDispensed.Value;

            if (dto.MmDispensedDaysSupply.HasValue) record.MmDispensedDaysSupply = dto.MmDispensedDaysSupply.Value;
            if (dto.ErxDaysSupply.HasValue) record.ErxDaysSupply = dto.ErxDaysSupply.Value;

            if (dto.MmDispensedSig != null) record.MmDispensedSig = dto.MmDispensedSig;
            if (dto.ErxSig != null) record.ErxSig = dto.ErxSig;

            if (dto.TelephoneNotes != null) record.TelephoneNotes = dto.TelephoneNotes;
            if (dto.TelephoneNotesDate.HasValue) record.TelephoneNotesDate = dto.TelephoneNotesDate.Value;
            if (dto.ErxNotes != null) record.ErxNotes = dto.ErxNotes;
            if (dto.ErxNotesDate.HasValue) record.ErxNotesDate = dto.ErxNotesDate.Value;
            if (dto.ErxNotesCreatedBy != null) record.ErxNotesCreatedBy = dto.ErxNotesCreatedBy;

            if (dto.MmDawCode != null) record.MmDawCode = dto.MmDawCode;
            if (dto.ErxDawCode != null) record.ErxDawCode = dto.ErxDawCode;

            if (dto.MmRefillsPharmacistEntered.HasValue) record.MmRefillsPharmacistEntered = dto.MmRefillsPharmacistEntered.Value;
            if (dto.ErxRefill.HasValue) record.ErxRefill = dto.ErxRefill.Value;
            if (dto.MmRphNotes != null) record.MmRphNotes = dto.MmRphNotes;
            if (dto.MmSignatureCaptured.HasValue) record.MmSignatureCaptured = dto.MmSignatureCaptured.Value;
            if (dto.FollowUpTag != null) record.FollowUpTag = dto.FollowUpTag;
            if (dto.MmDelivery != null) record.MmDelivery = dto.MmDelivery;
            if (dto.MmRxOrigin != null) record.MmRxOrigin = dto.MmRxOrigin;
            if (dto.MmCopay.HasValue) record.MmCopay = dto.MmCopay.Value;
            if (dto.PaymentMethod != null) record.PaymentMethod = dto.PaymentMethod;
            if (dto.InsCode != null) record.InsCode = dto.InsCode;
            if (dto.BinNo != null) record.BinNo = dto.BinNo;
            if (dto.TotalInsPaid.HasValue) record.TotalInsPaid = dto.TotalInsPaid.Value;
            if (dto.DurReasonCd != null) record.DurReasonCd = dto.DurReasonCd;
            if (dto.DurProfCd != null) record.DurProfCd = dto.DurProfCd;
            if (dto.DurResultCode != null) record.DurResultCode = dto.DurResultCode;
            if (dto.Message != null) record.Message = dto.Message;

            await _recordRepository.UpsertAsync(record, "RecordId");

            // Re-evaluate discrepancies if comparison engine is available
            if (_comparisonEngine != null)
            {
                var rules = (await _ruleRepository.GetAllAsync(r => r.IsActive)).ToList();
                var newDiscrepancies = _comparisonEngine.CompareRecord(record, rules);
                var newRuleIds = newDiscrepancies.Select(nd => nd.RuleId).ToHashSet();

                var existingDiscrepancies = (await _discrepancyRepository.GetAllAsync(d => d.RecordId == recordId)).ToList();

                foreach (var existing in existingDiscrepancies)
                {
                    var rule = rules.FirstOrDefault(r => r.RuleId == existing.RuleId);
                    if (!newRuleIds.Contains(existing.RuleId))
                    {
                        // Resolved by user's update!
                        if (existing.Status == "Open" || string.IsNullOrEmpty(existing.Status))
                        {
                            existing.Status = "Corrected";
                            existing.ReviewedBy = "User (Edited)";
                            existing.ReviewedAt = DateTime.UtcNow;
                        }
                        if (rule != null)
                        {
                            var currentMm = ErxComparisonEngine.GetPropertyValueAsString(record, rule.MmColumnName);
                            var currentErx = ErxComparisonEngine.GetPropertyValueAsString(record, rule.ErxColumnName);
                            if (currentMm != null) existing.MmValue = currentMm;
                            if (currentErx != null) existing.ErxValue = currentErx;
                        }
                        await _discrepancyRepository.UpsertAsync(existing, "DiscrepancyId");
                    }
                    else
                    {
                        var matchingNew = newDiscrepancies.First(nd => nd.RuleId == existing.RuleId);
                        existing.MmValue = matchingNew.MmValue;
                        existing.ErxValue = matchingNew.ErxValue;
                        await _discrepancyRepository.UpsertAsync(existing, "DiscrepancyId");
                    }
                }

                // Any new discrepancies created
                var existingRuleIds = existingDiscrepancies.Select(ed => ed.RuleId).ToHashSet();
                foreach (var newDisc in newDiscrepancies)
                {
                    if (!existingRuleIds.Contains(newDisc.RuleId))
                    {
                        newDisc.RecordId = recordId;
                        newDisc.CreatedAt = DateTime.UtcNow;
                        await _discrepancyRepository.AddAsync(newDisc);
                    }
                }

                // Update batch total discrepancies count
                var batch = await _batchRepository.GetByIdAsync(record.BatchId, "BatchId");
                if (batch != null)
                {
                    var bRecords = (await _recordRepository.GetAllAsync(r => r.BatchId == record.BatchId)).ToList();
                    var recordIds = bRecords.Select(r => r.RecordId).ToHashSet();
                    var bDiscs = await _discrepancyRepository.GetAllAsync(d => recordIds.Contains(d.RecordId));
                    batch.TotalDiscrepancies = bDiscs.Count(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status));
                    await _batchRepository.UpsertAsync(batch, "BatchId");
                }
            }

            var updatedDto = await GetRecordDiscrepanciesAsync(recordId);
            _logger.LogInformation("[AuditService:UpdateRecord] RecordId: {RecordId} updated successfully. Discrepancies remaining: {Count}.",
                recordId, updatedDto?.Discrepancies?.Count ?? 0);
            return SuccessResult<ErxAuditRecordDto>.Ok(updatedDto!, "Record updated successfully.");
        }
    }
}
