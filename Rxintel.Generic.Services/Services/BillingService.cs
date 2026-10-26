using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Models.Constants;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Helpers;
using Rxintel.Generic.Services.Interfaces;
using System.Linq.Expressions;
namespace Rxintel.Generic.Services.Services
{
    public class BillingService : GenericService<BillingDto, Billing>, IBillingService
    {
        private readonly IBillingRepository _billingRepository;
        private readonly IBillingFieldMappingRepository _billingFieldMappingRepository;
        private readonly IPharmacyRepository _pharmacyRepository;
        private readonly IPharmacySoftwareRepository _pharmacySoftwareRepository;
        private readonly IInsuranceRepository _insuranceRepository;
        private readonly IImportErrorService _importErrorService;
        private readonly IBackgroundTaskRunner _backgroundTaskRunner;
        private readonly IOpenFdaNdcResolver _openFdaNdcResolver;
        private readonly IValidator<BillingDto> _billingValidator;
        private readonly ITokenParser _tokenParser;
        private readonly IMapper _mapper;

        /// <summary>Fields whose validator messages are replaced by the CSV BIN resolver's message.</summary>
        private static readonly HashSet<string> BinSupersededFields =
            new(StringComparer.OrdinalIgnoreCase)
            {
                nameof(BillingDto.InsuranceId),
                nameof(BillingDto.PrimaryInsuranceBinNo)
            };

        private static readonly string[] BillingColumnExclusions =
        [
            "Id",
            "CreatedDate",
            "ModifiedDate",
            "PharmacyId",
            "InsuranceId",
            "SecondaryInsuranceId",
            "TotalInsurancePaidAmount",
        ];

        public BillingService(
            IBillingRepository billingRepository,
            IBillingFieldMappingRepository billingFieldMappingRepository,
            IPharmacyRepository pharmacyRepository,
            IPharmacySoftwareRepository pharmacySoftwareRepository,
            IInsuranceRepository insuranceRepository,
            IImportErrorService importErrorService,
            IBackgroundTaskRunner backgroundTaskRunner,
            IOpenFdaNdcResolver openFdaNdcResolver,
            IValidator<BillingDto> billingValidator,
            ITokenParser tokenParser,
            IMapper mapper,
            ILogger<BillingService> logger) : base(billingRepository, mapper, "Id")
        {
            _billingRepository = billingRepository;
            _billingFieldMappingRepository = billingFieldMappingRepository;
            _pharmacyRepository = pharmacyRepository;
            _pharmacySoftwareRepository = pharmacySoftwareRepository;
            _insuranceRepository = insuranceRepository;
            _importErrorService = importErrorService;
            _backgroundTaskRunner = backgroundTaskRunner;
            _openFdaNdcResolver = openFdaNdcResolver;
            _billingValidator = billingValidator;
            _tokenParser = tokenParser;
            _mapper = mapper;
        }
        public async Task<PagedList<BillingDto>> GetAll(PageParam pageParam, Guid? importErrorFileId = null)
        {
            var pharmacyId = _tokenParser.GetPharmacyId();
            Expression<Func<Billing, bool>>? filter = null;

            if (pharmacyId.HasValue && importErrorFileId.HasValue && importErrorFileId.Value != Guid.Empty)
                filter = b => b.PharmacyId == pharmacyId.Value && b.ImportErrorFileId == importErrorFileId.Value;
            else if (pharmacyId.HasValue)
                filter = b => b.PharmacyId == pharmacyId.Value;
            else if (importErrorFileId.HasValue && importErrorFileId.Value != Guid.Empty)
                filter = b => b.ImportErrorFileId == importErrorFileId.Value;

            var includes = new Expression<Func<Billing, object>>[] { b => b.Pharmacy!, b => b.Insurance!, b => b.SecondaryInsuranceProvider! };
            var hasSearch = !string.IsNullOrWhiteSpace(pageParam?.SearchText);

            if (!hasSearch && pageParam != null && pageParam.IsPaginationRequested)
            {
                var totalRecords = await _billingRepository.GetFilteredCountAsync(filter, includes);
                var pageEntities = (await _billingRepository.GetAllAsync(filter, includes, pageParam)).ToList();
                var pageData = _mapper.Map<List<BillingDto>>(pageEntities);
                return new PagedList<BillingDto>(pageData, pageParam, totalRecords);
            }

            var entities = (await _billingRepository.GetAllAsync(filter, includes)).ToList();

            entities = entities.FilterBySearch(
                pageParam?.SearchText,
                b => b.NDC,
                b => b.DrugName,
                b => b.QuantityBilled.ToString(),
                b => b.RXNO,
                b => b.BillDate.ToString(),
                b => b.TotalInsurancePaidAmount.ToString(),
                b => b.Pharmacy!.BusinessTradeName,
                b => b.Insurance!.InsuranceName);

            var data = _mapper.Map<List<BillingDto>>(entities);
            return ToPagedList(data, pageParam);
        }
        public Task<IEnumerable<string>> GetBillingColumns()
        {
            var columns = DtoColumnHelper.GetPublicPropertyNames(typeof(BillingDto), BillingColumnExclusions);
            return Task.FromResult(columns);
        }
        public async Task<(IReadOnlyList<BillingDto> Rows, IReadOnlyList<ImportErrorItemDto>? Errors)> ParseCsvFromUpload(
            IFormFile? file,
            Guid pharmacyId,
            Guid pharmacySoftwareId)
        {
            if (file == null || file.Length == 0)
                return (Array.Empty<BillingDto>(), ImportErrorHelper.SingleValidationFileError(CustomMessage.BillingPurchaseCsvFileRequired));

            var uploadedFileName = ImportFileHelper.GetUploadedFileName(file);
            if (!ImportFileHelper.IsSupportedImportFile(uploadedFileName))
                return (Array.Empty<BillingDto>(), ImportErrorHelper.SingleValidationFileError(CustomMessage.BillingPurchaseCsvFileRequired));

            if (pharmacySoftwareId == Guid.Empty)
            {
                var pharmacy = await _pharmacyRepository.GetByIdAsync(pharmacyId, "Id");

                if (pharmacy != null)
                {
                    // Existing Software
                    if (pharmacy.PharmacySoftwareId.HasValue)
                    {
                        pharmacySoftwareId = pharmacy.PharmacySoftwareId.Value;
                    }
                    // Other Software
                    else if (!string.IsNullOrWhiteSpace(pharmacy.PharmacyBillingSoftwareName))
                    {
                        var pharmacySoftware = (await _pharmacySoftwareRepository.GetAllAsync(
                               x => x.SoftwareName.Trim().ToLower() ==
                                    pharmacy.PharmacyBillingSoftwareName.Trim().ToLower()))
                        .FirstOrDefault();

                        if (pharmacySoftware != null)
                        {
                            pharmacySoftwareId = pharmacySoftware.Id;
                        }
                    }
                }
            }

            if (pharmacySoftwareId == Guid.Empty)
            {
                return (
                    Array.Empty<BillingDto>(),
                    ImportErrorHelper.SingleConfigurationFileError(
                        CustomMessage.BillingPharmacySoftwareNotConfigured));
            }

            List<BillingFieldMapping> mappings = (await _billingFieldMappingRepository.GetAllAsync(
                x => x.PharmacySoftwareId == pharmacySoftwareId)).ToList();

            if (mappings.Count == 0)
            {
                if (_tokenParser.IsEmployee())
                {
                    var uploaderName = _tokenParser.GetName();
                    var uploaderEmail = _tokenParser.GetEmail();
                    _backgroundTaskRunner.Enqueue(async sp =>
                    {
                        var notificationService = sp.GetRequiredService<IBillingFieldMappingNotificationService>();
                        await notificationService.NotifyAdminsMappingMissingAsync(
                            pharmacyId,
                            pharmacySoftwareId,
                            uploaderName,
                            uploaderEmail);
                    });
                }

                return (Array.Empty<BillingDto>(), ImportErrorHelper.SingleFieldMappingFileError(CustomMessage.BillingFieldMappingsNotFound));
            }

            await using var stream = file.OpenReadStream();
            var csvMappings = mappings
                .Where(m => !string.IsNullOrWhiteSpace(m.SourceFieldName))
                .Select(m => new CsvFieldMapping(m.SourceFieldName!, m.DestinationFieldName));
            var (parsedRows, columnMappingErrors) = CsvImportHelper.ParseWithRowNumbers<BillingDto>(
                stream, uploadedFileName, csvMappings, dto =>
                {
                    dto.PharmacyId = pharmacyId;
                });

            if (columnMappingErrors is { Count: > 0 })
                return (Array.Empty<BillingDto>(), columnMappingErrors);

            if (parsedRows.Count == 0)
                return (Array.Empty<BillingDto>(), ImportErrorHelper.SingleValidationFileError(CustomMessage.BillingCsvNoRowsFound));

            var binLookup = BillingCsvHelper.BuildInsuranceBinLookup(await _insuranceRepository.GetAllAsync());
            var errors = new List<ImportErrorItemDto>();
            var rows = new List<BillingDto>();

            foreach (var parsed in parsedRows)
            {
                var plainNdc = ValidationHelperBase.GetPlainNdcDigits(parsed.Dto.NDC);
                if (plainNdc != null)
                    parsed.Dto.NDC = plainNdc;

                var neededFdaLookup = ValidationHelperBase.NeedsFdaNdcLookup(parsed.Dto.NDC);
                if (neededFdaLookup)
                {
                    var correctedNdc = await _openFdaNdcResolver.ResolveToNdc11Async(parsed.Dto.NDC);
                    if (correctedNdc != null)
                        parsed.Dto.NDC = correctedNdc;
                }

                BillingCsvHelper.ApplyCalculatedFields(parsed.Dto);
                BillingCsvHelper.NormalizeBinNumbers(parsed.Dto);

                var binError = BillingCsvHelper.ResolveInsuranceFromCsvBin(parsed.RowNumber, parsed.Dto, binLookup);
                var validationErrors = ImportRowValidationHelper.CollectValidationErrors(
                    _billingValidator,
                    parsed.RowNumber,
                    parsed.Dto,
                    r => r.NDC,
                    r => r.RXNO).ToList();

                if (neededFdaLookup)
                {
                    ImportRowValidationHelper.AppendUnresolvedShortNdcError(
                        validationErrors,
                        parsed.RowNumber,
                        parsed.Dto,
                        r => r.NDC,
                        r => r.RXNO,
                        ModelValidationErrorCodes.INVALID_BILLING_NDC_FORMAT);
                }

                if (binError != null)
                {
                    // The BIN resolver already reported this row's BIN problem, so drop the
                    // validator's own take on the same two fields to avoid a duplicate message.
                    validationErrors.RemoveAll(e => BinSupersededFields.Contains(e.FieldName ?? string.Empty));
                    validationErrors.Insert(0, binError);
                }

                var rowErrors = new List<ImportErrorItemDto>();
                ImportErrorHelper.AppendWithoutParseDuplicates(rowErrors, parsed.ParseErrors, validationErrors);

                if (rowErrors.Count > 0)
                    errors.AddRange(rowErrors);
                else
                    rows.Add(parsed.Dto);
            }

            errors = ImportErrorHelper.Dedupe(errors);
            return (rows, errors.Count > 0 ? errors : null);
        }

        public async Task<SuccessResult<IEnumerable<BillingDto>>> BulkUpsertFromCsvUpload(
            IFormFile? file,
            Guid pharmacyId,
            Guid pharmacySoftwareId)
        {
            var uploadedFileName = ImportFileHelper.GetUploadedFileName(file);
            var (rows, errors) = await ParseCsvFromUpload(file, pharmacyId, pharmacySoftwareId);
            var hasErrors = errors != null && errors.Count > 0;
            var uploadedBy = _tokenParser.GetName() ?? _tokenParser.GetEmail();

            var importFile = await _importErrorService.RegisterImportBatchAsync(
                ImportType.Billing,
                pharmacyId,
                uploadedFileName,
                uploadedBy,
                errors);

            if (rows.Count == 0)
            {
                var failure = SuccessResult<IEnumerable<BillingDto>>.BadRequest(
                    hasErrors ? CustomMessage.CsvUploadValidationErrors : CustomMessage.BillingCsvNoRowsFound);

                if (hasErrors)
                    failure.Errors = ImportErrorHelper.ToErrorDetails(errors!);

                return failure;
            }

            foreach (var row in rows)
                row.ImportErrorFileId = importFile.Id;

            var result = await BulkUpsert(rows);

            if (result.IsSuccessStatusCode)
                await _importErrorService.UpdateTotalImportedRowsAsync(importFile.Id, rows.Count);

            if (hasErrors && result.IsSuccessStatusCode)
            {
                var failedRowCount = importFile.FailedRowCount;

                result.Message = CustomMessage.CsvUploadPartialSuccess(rows.Count, failedRowCount);
                result.Errors = ImportErrorHelper.ToErrorDetails(errors!);
            }

            return result;
        }

        public override async Task<SuccessResult<BillingDto>> UpsertAsync(BillingDto dto)
        {
            BillingCsvHelper.ApplyCalculatedFields(dto);
            await SyncSecondaryInsuranceFieldsAsync(dto);

            if (!dto.ImportErrorFileId.HasValue || dto.ImportErrorFileId == Guid.Empty)
            {
                var uploadedBy = _tokenParser.GetName() ?? _tokenParser.GetEmail();
                var importFile = await _importErrorService.GetOrCreateBillingSingleEntryFileAsync(
                    dto.PharmacyId,
                    uploadedBy);
                dto.ImportErrorFileId = importFile.Id;
            }

            var result = await base.UpsertAsync(dto);

            if (result.IsSuccessStatusCode && dto.ImportErrorFileId.HasValue)
                await RefreshImportFileTotalImportedRowsAsync(dto.ImportErrorFileId.Value);

            return result;
        }

        public override async Task<IEnumerable<BillingDto>> BulkUpsertAsync(IEnumerable<BillingDto> dtos)
        {
            if (dtos == null)
                return await base.BulkUpsertAsync(dtos!);

            var list = dtos as IList<BillingDto> ?? dtos.ToList();
            foreach (var dto in list)
            {
                BillingCsvHelper.ApplyCalculatedFields(dto);
                await SyncSecondaryInsuranceFieldsAsync(dto);
            }

            return await base.BulkUpsertAsync(list);
        }

        private async Task RefreshImportFileTotalImportedRowsAsync(Guid importErrorFileId)
        {
            var count = await _billingRepository.GetFilteredCountAsync(
                b => b.ImportErrorFileId == importErrorFileId);
            await _importErrorService.UpdateTotalImportedRowsAsync(importErrorFileId, count);
        }

        private async Task SyncSecondaryInsuranceFieldsAsync(BillingDto dto)
        {
            if (dto == null)
                return;

            if (!dto.SecondaryInsuranceId.HasValue || dto.SecondaryInsuranceId == Guid.Empty)
            {
                dto.SecondaryInsuranceId = null;
                if (dto.SecondaryInsurancePaidAmount.GetValueOrDefault() <= 0)
                    dto.SecondaryInsurance = null;
                return;
            }

            var insurance = await _insuranceRepository.GetByIdAsync(dto.SecondaryInsuranceId.Value, "Id");
            if (insurance != null)
                dto.SecondaryInsurance = insurance.InsuranceName;
        }
    }
}
