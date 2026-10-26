using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Api;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.AI.Controllers
{
    [ApiController]
    [Route("api/erx-audit")]
    public class ErxAuditController : BaseController
    {
        private readonly IErxAuditService _auditService;
        private readonly IErxAuditImportService _importService;
        private readonly ILogger<ErxAuditController> _logger;
        private readonly ITokenParser _tokenParser;

        public ErxAuditController(
            IErxAuditService auditService,
            IErxAuditImportService importService,
            ILogger<ErxAuditController> logger,
            ITokenParser tokenParser)
        {
            _auditService = auditService;
            _importService = importService;
            _logger = logger;
            _tokenParser = tokenParser;
        }

        [HttpPost("batches")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<SuccessResult<ErxAuditBatchUploadResponseDto>>> UploadBatch(
            IFormFile file,
            [FromQuery] Guid? pharmacyId = null)
        {
            if (!pharmacyId.HasValue && Request.HasFormContentType && Request.Form.TryGetValue("pharmacyId", out var formVal) && Guid.TryParse(formVal, out var parsed))
            {
                pharmacyId = parsed;
            }

            _logger.LogInformation("[Audit:UploadBatch] Received batch upload request for file '{FileName}' ({Length} bytes), PharmacyId: {PharmacyId}.",
                file?.FileName, file?.Length ?? 0, pharmacyId);

            var result = await _importService.UploadAndProcessBatchAsync(file, null, pharmacyId);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Audit:UploadBatch] Failed processing file '{FileName}': {Message}",
                    file?.FileName, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("[Audit:UploadBatch] Successfully uploaded and processed '{FileName}'. BatchId: {BatchId}, PharmacyId: {PharmacyId}, TotalRows: {TotalRows}, Discrepancies: {TotalDiscrepancies}.",
                file?.FileName, result.Data?.BatchId, result.Data?.PharmacyId, result.Data?.TotalRows, result.Data?.TotalDiscrepancies);

            return Ok(result);
        }

        [HttpGet("batches")]
        public async Task<ActionResult<PagedList<ErxAuditBatchDto>>> GetBatches(
            [FromQuery] PageParam pageParam,
            [FromQuery] Guid? pharmacyId = null)
        {
            _logger.LogInformation("[Audit:GetBatches] Fetching batches (PharmacyId: {PharmacyId}, Page: {Page}, PageSize: {PageSize}, Search: '{SearchText}').",
                pharmacyId, pageParam?.Page, pageParam?.PageSize, pageParam?.SearchText);

            var batches = await _auditService.GetBatchesAsync(pageParam, pharmacyId);

            _logger.LogInformation("[Audit:GetBatches] Retrieved {Count} batches (Total: {TotalRecords}).",
                batches?.Data?.Count() ?? 0, batches?.TotalRecords ?? 0);

            return Ok(batches);
        }

        [HttpGet("records")]
        public async Task<ActionResult<PagedList<ErxAuditRecordDto>>> GetAllRecords(
            [FromQuery] Guid? batchId,
            [FromQuery] PageParam pageParam,
            [FromQuery] bool? discrepanciesOnly = null,
            [FromQuery] string? discrepancyColumn = null,
            [FromQuery] int? ruleId = null,
            [FromQuery] string? paymentMethod = null,
            [FromQuery] string? insCode = null,
            [FromQuery] string? binNo = null,
            [FromQuery] decimal? minTotalPaid = null,
            [FromQuery] decimal? maxTotalPaid = null,
            [FromQuery] bool? isHighValue = null,
            [FromQuery] string? rxOrigin = null,
            [FromQuery] bool? nonElectronicOnly = null,
            [FromQuery] Guid? pharmacyId = null)
        {
            _logger.LogInformation("[Audit:GetAllRecords] Querying records (BatchId: {BatchId}, PharmacyId: {PharmacyId}, Page: {Page}, PageSize: {PageSize}, DiscrepanciesOnly: {DiscrepanciesOnly}, Column: '{Column}', RuleId: {RuleId}, PaymentMethod: '{PaymentMethod}', InsCode: '{InsCode}', BinNo: '{BinNo}', HighValue: {HighValue}, RxOrigin: '{RxOrigin}', NonElectronic: {NonElectronic}).",
                batchId, pharmacyId, pageParam?.Page, pageParam?.PageSize, discrepanciesOnly, discrepancyColumn, ruleId, paymentMethod, insCode, binNo, isHighValue, rxOrigin, nonElectronicOnly);

            var records = await _auditService.GetAllRecordsAsync(
                batchId, pageParam, discrepanciesOnly, discrepancyColumn, ruleId,
                paymentMethod, insCode, binNo, minTotalPaid, maxTotalPaid, isHighValue, rxOrigin, nonElectronicOnly, pharmacyId);

            _logger.LogInformation("[Audit:GetAllRecords] Retrieved {Count} records (Total: {TotalRecords}) for BatchId: {BatchId}, PharmacyId: {PharmacyId}.",
                records?.Data?.Count() ?? 0, records?.TotalRecords ?? 0, batchId, pharmacyId);

            return Ok(records);
        }

        [HttpGet("filter-options")]
        public async Task<ActionResult<ErxAuditFilterOptionsDto>> GetFilterOptions(
            [FromQuery] Guid? batchId = null,
            [FromQuery] Guid? pharmacyId = null)
        {
            _logger.LogInformation("[Audit:GetFilterOptions] Requesting filter options for BatchId: {BatchId}, PharmacyId: {PharmacyId}.", batchId, pharmacyId);
            var options = await _auditService.GetFilterOptionsAsync(batchId, pharmacyId);
            return Ok(options);
        }

        [HttpPost("seed-sample-data")]
        public async Task<ActionResult<SuccessResult<ErxAuditBatchUploadResponseDto>>> SeedSampleData()
        {
            _logger.LogInformation("[Audit:SeedSampleData] Request received to seed sample audit data.");

            var result = await _importService.SeedSampleDataAsync();
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Audit:SeedSampleData] Failed to seed sample data: {Message}", result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("[Audit:SeedSampleData] Sample data seeded successfully. BatchId: {BatchId}, TotalRows: {TotalRows}, Discrepancies: {TotalDiscrepancies}.",
                result.Data?.BatchId, result.Data?.TotalRows, result.Data?.TotalDiscrepancies);

            return Ok(result);
        }

        [HttpGet("batches/{batchId}")]
        public async Task<ActionResult<ErxAuditBatchDto>> GetBatchById(Guid batchId)
        {
            _logger.LogInformation("[Audit:GetBatchById] Request for BatchId: {BatchId}.", batchId);

            var batch = await _auditService.GetBatchByIdAsync(batchId);
            if (batch == null)
            {
                _logger.LogWarning("[Audit:GetBatchById] Batch {BatchId} not found.", batchId);
                return NotFound();
            }

            _logger.LogInformation("[Audit:GetBatchById] Batch {BatchId} ('{FileName}') retrieved successfully with {TotalRows} rows.",
                batchId, batch.FileName, batch.TotalRows);

            return Ok(batch);
        }

        [HttpDelete("batches/{batchId}")]
        public async Task<ActionResult<SuccessResult<string>>> DeleteBatch(Guid batchId)
        {
            _logger.LogInformation("[Audit:DeleteBatch] Request to delete BatchId: {BatchId}.", batchId);

            var result = await _auditService.DeleteBatchAsync(batchId);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Audit:DeleteBatch] Failed to delete BatchId: {BatchId}: {Message}", batchId, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("[Audit:DeleteBatch] BatchId: {BatchId} ('{FileName}') deleted successfully.", batchId, result.Data);

            return Ok(result);
        }

        [HttpGet("batches/{batchId}/records")]
        public async Task<ActionResult<PagedList<ErxAuditRecordDto>>> GetBatchRecords(
            Guid batchId,
            [FromQuery] PageParam pageParam,
            [FromQuery] bool? discrepanciesOnly = null,
            [FromQuery] string? discrepancyColumn = null,
            [FromQuery] int? ruleId = null,
            [FromQuery] string? paymentMethod = null,
            [FromQuery] string? insCode = null,
            [FromQuery] string? binNo = null,
            [FromQuery] decimal? minTotalPaid = null,
            [FromQuery] decimal? maxTotalPaid = null,
            [FromQuery] bool? isHighValue = null,
            [FromQuery] string? rxOrigin = null,
            [FromQuery] bool? nonElectronicOnly = null,
            [FromQuery] Guid? pharmacyId = null)
        {
            _logger.LogInformation("[Audit:GetBatchRecords] Request for BatchId: {BatchId}, PharmacyId: {PharmacyId} (Page: {Page}, PageSize: {PageSize}, DiscrepanciesOnly: {DiscrepanciesOnly}, Column: '{Column}', RuleId: {RuleId}, PaymentMethod: '{PaymentMethod}', InsCode: '{InsCode}', BinNo: '{BinNo}', HighValue: {HighValue}, RxOrigin: '{RxOrigin}', NonElectronic: {NonElectronic}).",
                batchId, pharmacyId, pageParam?.Page, pageParam?.PageSize, discrepanciesOnly, discrepancyColumn, ruleId, paymentMethod, insCode, binNo, isHighValue, rxOrigin, nonElectronicOnly);

            var records = await _auditService.GetBatchRecordsAsync(
                batchId, pageParam, discrepanciesOnly, discrepancyColumn, ruleId,
                paymentMethod, insCode, binNo, minTotalPaid, maxTotalPaid, isHighValue, rxOrigin, nonElectronicOnly, pharmacyId);

            _logger.LogInformation("[Audit:GetBatchRecords] Retrieved {Count} records for BatchId: {BatchId} (Total: {TotalRecords}).",
                records?.Data?.Count() ?? 0, batchId, records?.TotalRecords ?? 0);

            return Ok(records);
        }

        [HttpGet("discrepancies/summary")]
        public async Task<ActionResult<List<ErxDiscrepancyColumnSummaryDto>>> GetDiscrepancySummary(
            [FromQuery] Guid? batchId = null,
            [FromQuery] Guid? pharmacyId = null)
        {
            _logger.LogInformation("[Audit:GetDiscrepancySummary] Requesting column discrepancy summary for BatchId: {BatchId}, PharmacyId: {PharmacyId}.", batchId, pharmacyId);

            var summary = await _auditService.GetDiscrepancySummaryAsync(batchId, pharmacyId);

            _logger.LogInformation("[Audit:GetDiscrepancySummary] Retrieved {Count} column discrepancy summaries for BatchId: {BatchId}.",
                summary?.Count ?? 0, batchId);

            return Ok(summary);
        }

        [HttpGet("records/{recordId}/discrepancies")]
        public async Task<ActionResult<ErxAuditRecordDto>> GetRecordDiscrepancies(long recordId)
        {
            _logger.LogInformation("[Audit:GetRecordDiscrepancies] Request for RecordId: {RecordId}.", recordId);

            var record = await _auditService.GetRecordDiscrepanciesAsync(recordId);
            if (record == null)
            {
                _logger.LogWarning("[Audit:GetRecordDiscrepancies] RecordId: {RecordId} not found.", recordId);
                return NotFound();
            }

            _logger.LogInformation("[Audit:GetRecordDiscrepancies] RecordId: {RecordId} (RxNumber: '{RxNumber}') retrieved with {DiscrepancyCount} discrepancies.",
                recordId, record.RxNumber, record.Discrepancies?.Count ?? 0);

            return Ok(record);
        }

        [HttpPut("records/{recordId}")]
        public async Task<ActionResult<SuccessResult<ErxAuditRecordDto>>> UpdateRecord(
            long recordId,
            [FromBody] UpdateErxAuditRecordDto dto)
        {
            _logger.LogInformation("[Audit:UpdateRecord] Updating RecordId: {RecordId} (RxNumber: '{RxNumber}', Drug: '{DrugName}').",
                recordId, dto?.RxNumber, dto?.MmDispensedDrugName ?? dto?.ErxDrugName);

            var result = await _auditService.UpdateRecordAsync(recordId, dto);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Audit:UpdateRecord] Failed to update RecordId: {RecordId}: {Message}", recordId, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("[Audit:UpdateRecord] Successfully updated RecordId: {RecordId}. Remaining discrepancies: {Count}.",
                recordId, result.Data?.Discrepancies?.Count ?? 0);

            return Ok(result);
        }

        [HttpPut("discrepancies/{id}/status")]
        public async Task<ActionResult<SuccessResult<ErxAuditDiscrepancyDto>>> UpdateDiscrepancyStatus(
            long id,
            [FromBody] UpdateDiscrepancyStatusDto dto)
        {
            _logger.LogInformation("[Audit:UpdateDiscrepancyStatus] Updating DiscrepancyId: {DiscrepancyId} to '{Status}' by '{ReviewedBy}' (MmValue: '{MmVal}', ErxValue: '{ErxVal}').",
                id, dto?.Status, dto?.ReviewedBy, dto?.MmValue, dto?.ErxValue);

            var result = await _auditService.UpdateDiscrepancyStatusAsync(id, dto.Status, dto.ReviewedBy, dto.ErxValue, dto.MmValue);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Audit:UpdateDiscrepancyStatus] Failed to update DiscrepancyId: {DiscrepancyId}: {Message}", id, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("[Audit:UpdateDiscrepancyStatus] DiscrepancyId: {DiscrepancyId} updated to '{Status}'. Remaining record: {RemRec}, Remaining batch: {RemBatch}.",
                id, dto?.Status, result.Data?.RemainingRecordDiscrepancies, result.Data?.RemainingBatchDiscrepancies);

            return Ok(result);
        }

        [HttpPost("discrepancies/bulk-status")]
        public async Task<ActionResult<SuccessResult<BulkUpdateDiscrepancyResponseDto>>> BulkUpdateDiscrepancyStatus(
            [FromBody] BulkUpdateDiscrepancyStatusDto dto)
        {
            _logger.LogInformation("[Audit:BulkUpdateDiscrepancyStatus] Request received (BatchId: {BatchId}, Status: '{Status}', Column: '{Column}', RuleId: {RuleId}, DiscrepanciesCount: {Count}).",
                dto?.BatchId, dto?.Status, dto?.DiscrepancyColumn, dto?.RuleId, dto?.DiscrepancyIds?.Count ?? 0);

            var result = await _auditService.BulkUpdateDiscrepancyStatusAsync(dto);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Audit:BulkUpdateDiscrepancyStatus] Failed bulk status update: {Message}", result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("[Audit:BulkUpdateDiscrepancyStatus] Bulk update successful. Updated: {UpdatedCount}, RemainingBatch: {Remaining}.",
                result.Data?.UpdatedCount, result.Data?.RemainingBatchDiscrepancies);

            return Ok(result);
        }

        [HttpPost("resolve-matching-demographics")]
        public async Task<ActionResult<SuccessResult<string>>> ResolveMatchingDemographics([FromQuery] Guid? batchId = null)
        {
            _logger.LogInformation("[Audit:ResolveMatchingDemographics] Request to auto-resolve matching demographics for BatchId: {BatchId}.", batchId);

            var count = await _auditService.ResolvePatientNameMatchingDemographicsAsync(batchId);

            _logger.LogInformation("[Audit:ResolveMatchingDemographics] Auto-resolved {Count} Patient Name discrepancies for BatchId: {BatchId}.", count, batchId);

            return Ok(SuccessResult<string>.Ok(count.ToString(), $"Resolved {count} Patient Name discrepancies where Patient DOB, Phone, and Address all match."));
        }

        [HttpPost("cleanup-duplicate-rules")]
        public async Task<ActionResult<SuccessResult<string>>> CleanupDuplicateRules([FromQuery] Guid? batchId = null)
        {
            if (!_tokenParser.IsAdmin())
            {
                _logger.LogWarning("[Audit:CleanupDuplicateRules] Access denied. Only system administrators can cleanup comparison rules.");
                return StatusCode(StatusCodes.Status403Forbidden, new SuccessResult<string>
                {
                    Status = StatusCodes.Status403Forbidden,
                    Message = "Access denied. Only system administrators can cleanup comparison rules."
                });
            }

            _logger.LogInformation("[Audit:CleanupDuplicateRules] Request to clean up duplicate rules and recalculate discrepancies for BatchId: {BatchId}.", batchId);

            var count = await _auditService.CleanupDuplicatePatientNameRulesAsync(batchId);

            _logger.LogInformation("[Audit:CleanupDuplicateRules] Cleaned up {Count} duplicate rules for BatchId: {BatchId}.", count, batchId);

            return Ok(SuccessResult<string>.Ok(count.ToString(), $"Cleaned up {count} duplicate rules and recalculated batch discrepancies."));
        }

        [HttpPost("resolve-days-supply-bud")]
        public async Task<ActionResult<SuccessResult<string>>> ResolveDaysSupplyBud([FromQuery] Guid? batchId = null)
        {
            _logger.LogInformation("[Audit:ResolveDaysSupplyBud] Request to auto-resolve compliant Days Supply discrepancies (Multi-Dose Vial BUD / 2:1 ratio) for BatchId: {BatchId}.", batchId);

            var count = await _auditService.ResolveDaysSupplyCompliantDiscrepanciesAsync(batchId);

            _logger.LogInformation("[Audit:ResolveDaysSupplyBud] Auto-resolved {Count} Days Supply discrepancies for BatchId: {BatchId}.", count, batchId);

            return Ok(SuccessResult<string>.Ok(count.ToString(), $"Resolved {count} Days Supply discrepancies compliant with Multi-Dose Vial Beyond-Use-Date (USP <797>) and volumetric ratio rules."));
        }

        [HttpGet("rules")]
        public async Task<ActionResult<PagedList<ErxColumnMatchRuleDto>>> GetRules(
            [FromQuery] PageParam pageParam,
            [FromQuery] int? insuranceCompanyId = null)
        {
            _logger.LogInformation(
                "[Audit:GetRules] Requesting rules (InsuranceCompanyId: {InsuranceCompanyId}, Page: {Page}, PageSize: {PageSize}, Search: '{SearchText}').",
                insuranceCompanyId, pageParam?.Page, pageParam?.PageSize, pageParam?.SearchText);

            var rules = await _auditService.GetRulesAsync(pageParam, insuranceCompanyId);

            _logger.LogInformation(
                "[Audit:GetRules] Retrieved {Count} rules (Total: {TotalRecords}, InsuranceCompanyId: {InsuranceCompanyId}).",
                rules?.Data?.Count ?? 0, rules?.TotalRecords ?? 0, insuranceCompanyId);

            return Ok(rules);
        }

        [HttpPost("rules")]
        public async Task<ActionResult<SuccessResult<ErxColumnMatchRuleDto>>> CreateRule([FromBody] ErxColumnMatchRuleDto dto)
        {
            if (!_tokenParser.IsAdmin())
            {
                _logger.LogWarning("[Audit:CreateRule] Access denied. Only system administrators can create comparison rules.");
                return StatusCode(StatusCodes.Status403Forbidden, new SuccessResult<ErxColumnMatchRuleDto>
                {
                    Status = StatusCodes.Status403Forbidden,
                    Message = "Access denied. Only system administrators can create comparison rules."
                });
            }

            _logger.LogInformation("[Audit:CreateRule] Request to create rule (FieldGroupName: '{FieldGroupName}', MmColumn: '{MmCol}', ErxColumn: '{ErxCol}', MatchType: '{MatchType}').",
                dto?.FieldGroupName, dto?.MmColumnName, dto?.ErxColumnName, dto?.MatchType);

            var result = await _auditService.CreateRuleAsync(dto);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Audit:CreateRule] Failed to create rule: {Message}", result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("[Audit:CreateRule] Successfully created rule {RuleId} for '{FieldGroupName}'.",
                result.Data?.RuleId, dto?.FieldGroupName);

            return Ok(result);
        }

        [HttpPut("rules/{id}")]
        public async Task<ActionResult<SuccessResult<ErxColumnMatchRuleDto>>> UpdateRule(int id, [FromBody] ErxColumnMatchRuleDto dto)
        {
            if (!_tokenParser.IsAdmin())
            {
                _logger.LogWarning("[Audit:UpdateRule] Access denied. Only system administrators can update comparison rules.");
                return StatusCode(StatusCodes.Status403Forbidden, new SuccessResult<ErxColumnMatchRuleDto>
                {
                    Status = StatusCodes.Status403Forbidden,
                    Message = "Access denied. Only system administrators can update comparison rules."
                });
            }

            _logger.LogInformation("[Audit:UpdateRule] Request to update RuleId: {RuleId} (FieldGroupName: '{FieldGroupName}').", id, dto?.FieldGroupName);

            var result = await _auditService.UpdateRuleAsync(id, dto);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Audit:UpdateRule] Failed to update RuleId: {RuleId}: {Message}", id, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("[Audit:UpdateRule] RuleId: {RuleId} updated successfully.", id);

            return Ok(result);
        }

        [HttpDelete("rules/{id}")]
        public async Task<ActionResult<SuccessResult<string>>> DeleteRule(int id)
        {
            if (!_tokenParser.IsAdmin())
            {
                _logger.LogWarning("[Audit:DeleteRule] Access denied. Only system administrators can delete comparison rules.");
                return StatusCode(StatusCodes.Status403Forbidden, new SuccessResult<string>
                {
                    Status = StatusCodes.Status403Forbidden,
                    Message = "Access denied. Only system administrators can delete comparison rules."
                });
            }

            _logger.LogInformation("[Audit:DeleteRule] Request to delete RuleId: {RuleId}.", id);

            var result = await _auditService.DeleteRuleAsync(id);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Audit:DeleteRule] Failed to delete RuleId: {RuleId}: {Message}", id, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("[Audit:DeleteRule] RuleId: {RuleId} ('{FieldGroupName}') deleted successfully.", id, result.Data);

            return Ok(result);
        }

        [HttpGet("insurance-companies")]
        public async Task<ActionResult<IEnumerable<InsuranceCompanyDto>>> GetInsuranceCompanies()
        {
            _logger.LogInformation("[Audit:GetInsuranceCompanies] Requesting insurance companies for audit rules.");

            var companies = await _auditService.GetInsuranceCompaniesAsync();
            var count = companies?.Count() ?? 0;

            _logger.LogInformation("[Audit:GetInsuranceCompanies] Retrieved {Count} insurance companies.", count);

            return Ok(companies);
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportRecords(
            [FromQuery] Guid? batchId,
            [FromQuery] bool? discrepanciesOnly = null,
            [FromQuery] string format = "xlsx",
            [FromQuery] Guid? pharmacyId = null)
        {
            _logger.LogInformation("[Audit:ExportRecords] Export request (BatchId: {BatchId}, PharmacyId: {PharmacyId}, DiscrepanciesOnly: {DiscrepanciesOnly}, Format: '{Format}').",
                batchId, pharmacyId, discrepanciesOnly, format);

            var bytes = await _auditService.ExportRecordsAsync(batchId, discrepanciesOnly, format, pharmacyId);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            _logger.LogInformation("[Audit:ExportRecords] Export generated successfully ({Bytes} bytes, Format: '{Format}').", bytes?.Length ?? 0, format);

            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = $"ERx_Audit_Records_{timestamp}.csv";
                return File(bytes, "text/csv", fileName);
            }
            else
            {
                var fileName = $"ERx_Audit_Records_{timestamp}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [HttpGet("batches/{batchId:guid}/export")]
        public async Task<IActionResult> ExportBatchRecords(
            Guid batchId,
            [FromQuery] bool? discrepanciesOnly = null,
            [FromQuery] string format = "xlsx",
            [FromQuery] Guid? pharmacyId = null)
        {
            return await ExportRecords(batchId, discrepanciesOnly, format, pharmacyId);
        }
    }
}
