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
    public class PurchaseService : GenericService<PurchaseDto, Purchase>, IPurchaseService
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IWholesalerFieldMappingRepository _wholesalerFieldMappingRepository;
        private readonly IImportErrorService _importErrorService;
        private readonly IOpenFdaNdcResolver _openFdaNdcResolver;
        private readonly IValidator<PurchaseDto> _purchaseValidator;
        private readonly ITokenParser _tokenParser;
        private readonly IBackgroundTaskRunner _backgroundTaskRunner;
        private readonly IMapper _mapper;

        private static readonly string[] PurchaseColumnExclusions =
        [
            "Id",
            "CreatedDate",
            "ModifiedDate",
            "PharmacyId",
            "WholesalerId",
            "TotalQuantityOrdered",
        ];

        public PurchaseService(
            IPurchaseRepository purchaseRepository,
            IWholesalerFieldMappingRepository wholesalerFieldMappingRepository,
            IImportErrorService importErrorService,
            IOpenFdaNdcResolver openFdaNdcResolver,
            IValidator<PurchaseDto> purchaseValidator,
            ITokenParser tokenParser,
            IBackgroundTaskRunner backgroundTaskRunner,
            IMapper mapper,
            ILogger<PurchaseService> logger) : base(purchaseRepository, mapper, "Id")
        {
            _purchaseRepository = purchaseRepository;
            _wholesalerFieldMappingRepository = wholesalerFieldMappingRepository;
            _importErrorService = importErrorService;
            _openFdaNdcResolver = openFdaNdcResolver;
            _purchaseValidator = purchaseValidator;
            _tokenParser = tokenParser;
            _backgroundTaskRunner = backgroundTaskRunner;
            _mapper = mapper;
        }
        public async Task<PagedList<PurchaseDto>> GetAll(PageParam pageParam, Guid? importErrorFileId = null)
        {
            var pharmacyId = _tokenParser.GetPharmacyId();
            Expression<Func<Purchase, bool>>? filter = null;

            if (pharmacyId.HasValue && importErrorFileId.HasValue && importErrorFileId.Value != Guid.Empty)
                filter = p => p.PharmacyId == pharmacyId.Value && p.ImportErrorFileId == importErrorFileId.Value;
            else if (pharmacyId.HasValue)
                filter = p => p.PharmacyId == pharmacyId.Value;
            else if (importErrorFileId.HasValue && importErrorFileId.Value != Guid.Empty)
                filter = p => p.ImportErrorFileId == importErrorFileId.Value;

            var includes = new Expression<Func<Purchase, object>>[] { p => p.Wholesaler!, p => p.Pharmacy! };
            var hasSearch = !string.IsNullOrWhiteSpace(pageParam?.SearchText);

            if (!hasSearch && pageParam != null && pageParam.IsPaginationRequested)
            {
                var totalRecords = await _purchaseRepository.GetFilteredCountAsync(filter, includes);
                var pageEntities = (await _purchaseRepository.GetAllAsync(filter, includes, pageParam)).ToList();
                var pageData = _mapper.Map<List<PurchaseDto>>(pageEntities);
                return new PagedList<PurchaseDto>(pageData, pageParam, totalRecords);
            }

            var entities = (await _purchaseRepository.GetAllAsync(filter, includes)).ToList();

            entities = entities.FilterBySearch(
                pageParam?.SearchText,
                p => p.NDC,
                p => p.QuantityPurchased.ToString(),
                p => p.TotalQuantityOrdered.ToString(),
                p => p.PackageSize,
                p => p.Description,
                p => p.InvoiceDate.ToString(),
                p => p.InvoiceNumber,
                p => p.Wholesaler?.WholesalerName,
                p => p.Pharmacy?.BusinessTradeName);

            var data = _mapper.Map<List<PurchaseDto>>(entities);
            return ToPagedList(data, pageParam);
        }

        public Task<IEnumerable<string>> GetPurchaseColumns()
        {
            var columns = DtoColumnHelper.GetPublicPropertyNames(typeof(PurchaseDto), PurchaseColumnExclusions);
            return Task.FromResult(columns);
        }

        public async Task<(IReadOnlyList<PurchaseDto> Rows, IReadOnlyList<ImportErrorItemDto>? Errors)> ParseCsvFromUpload(
            IFormFile? file,
            Guid pharmacyId,
            Guid wholesalerId)
        {
            if (file == null || file.Length == 0)
                return (Array.Empty<PurchaseDto>(), ImportErrorHelper.SingleValidationFileError(CustomMessage.BillingPurchaseCsvFileRequired));

            var uploadedFileName = ImportFileHelper.GetUploadedFileName(file);
            if (!ImportFileHelper.IsSupportedImportFile(uploadedFileName))
                return (Array.Empty<PurchaseDto>(), ImportErrorHelper.SingleValidationFileError(CustomMessage.BillingPurchaseCsvFileRequired));

            List<WholesalerFieldMapping> mappings = (await _wholesalerFieldMappingRepository.GetAllAsync(
                x => x.WholesalerId == wholesalerId)).ToList();

            if (mappings.Count == 0)
            {
                if (_tokenParser.IsEmployee())
                {
                    var uploaderName = _tokenParser.GetName();
                    var uploaderEmail = _tokenParser.GetEmail();
                    _backgroundTaskRunner.Enqueue(async sp =>
                    {
                        var notificationService = sp.GetRequiredService<IWholesalerFieldMappingNotificationService>();
                        await notificationService.NotifyAdminsMappingMissingAsync(
                            pharmacyId,
                            wholesalerId,
                            uploaderName,
                            uploaderEmail);
                    });
                }

                return (
                    Array.Empty<PurchaseDto>(),
                    ImportErrorHelper.SingleFieldMappingFileError(CustomMessage.PurchaseWholesalerFieldMappingsNotFound));
            }

            await using var stream = file.OpenReadStream();
            var csvMappings = mappings
                .Where(m => !string.IsNullOrWhiteSpace(m.SourceColumnName))
                .Select(m => new CsvFieldMapping(m.SourceColumnName, m.DestinationColumnName));
            var (parsedRows, columnMappingErrors) = CsvImportHelper.ParseWithRowNumbers<PurchaseDto>(
                stream, uploadedFileName, csvMappings, dto =>
                {
                    dto.PharmacyId = pharmacyId;
                    dto.WholesalerId = wholesalerId;
                });

            if (columnMappingErrors is { Count: > 0 })
                return (Array.Empty<PurchaseDto>(), columnMappingErrors);

            if (parsedRows.Count == 0)
                return (
                    Array.Empty<PurchaseDto>(),
                    ImportErrorHelper.SingleValidationFileError(CustomMessage.PurchaseCsvNoRowsFound));

            var errors = new List<ImportErrorItemDto>();
            var rows = new List<PurchaseDto>();

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

                PurchaseCsvHelper.ApplyCalculatedFields(parsed.Dto);

                var validationErrors = ImportRowValidationHelper.CollectValidationErrors(
                    _purchaseValidator,
                    parsed.RowNumber,
                    parsed.Dto,
                    r => r.NDC,
                    r => r.InvoiceNumber).ToList();

                if (neededFdaLookup)
                {
                    ImportRowValidationHelper.AppendUnresolvedShortNdcError(
                        validationErrors,
                        parsed.RowNumber,
                        parsed.Dto,
                        r => r.NDC,
                        r => r.InvoiceNumber,
                        ModelValidationErrorCodes.INVALID_PURCHASE_NDC_FORMAT);
                }

                var rowErrors = new List<ImportErrorItemDto>();
                ImportErrorHelper.AppendWithoutParseDuplicates(
                    rowErrors,
                    parsed.ParseErrors,
                    validationErrors);

                if (rowErrors.Count > 0)
                    errors.AddRange(rowErrors);
                else
                    rows.Add(parsed.Dto);
            }

            errors = ImportErrorHelper.Dedupe(errors);
            return (rows, errors.Count > 0 ? errors : null);
        }

        public async Task<SuccessResult<IEnumerable<PurchaseDto>>> BulkUpsertFromCsvUpload(
            IFormFile? file,
            Guid pharmacyId,
            Guid wholesalerId)
        {
            var uploadedFileName = ImportFileHelper.GetUploadedFileName(file);
            var (rows, errors) = await ParseCsvFromUpload(file, pharmacyId, wholesalerId);
            var hasErrors = errors != null && errors.Count > 0;
            var uploadedBy = _tokenParser.GetName() ?? _tokenParser.GetEmail();

            var importFile = await _importErrorService.RegisterImportBatchAsync(
                ImportType.Purchase,
                pharmacyId,
                uploadedFileName,
                uploadedBy,
                errors);

            if (rows.Count == 0)
            {
                var failure = SuccessResult<IEnumerable<PurchaseDto>>.BadRequest(
                    hasErrors ? CustomMessage.CsvUploadValidationErrors : CustomMessage.PurchaseCsvNoRowsFound);

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

        public override async Task<SuccessResult<PurchaseDto>> UpsertAsync(PurchaseDto dto)
        {
            PurchaseCsvHelper.ApplyCalculatedFields(dto);

            if (!dto.ImportErrorFileId.HasValue || dto.ImportErrorFileId == Guid.Empty)
            {
                var uploadedBy = _tokenParser.GetName() ?? _tokenParser.GetEmail();
                var importFile = await _importErrorService.GetOrCreatePurchaseSingleEntryFileAsync(
                    dto.PharmacyId,
                    uploadedBy);
                dto.ImportErrorFileId = importFile.Id;
            }

            var result = await base.UpsertAsync(dto);

            if (result.IsSuccessStatusCode && dto.ImportErrorFileId.HasValue)
                await RefreshImportFileTotalImportedRowsAsync(dto.ImportErrorFileId.Value);

            return result;
        }

        private async Task RefreshImportFileTotalImportedRowsAsync(Guid importErrorFileId)
        {
            var count = await _purchaseRepository.GetFilteredCountAsync(
                p => p.ImportErrorFileId == importErrorFileId);
            await _importErrorService.UpdateTotalImportedRowsAsync(importErrorFileId, count);
        }

        public override async Task<IEnumerable<PurchaseDto>> BulkUpsertAsync(IEnumerable<PurchaseDto> dtos)
        {
            if (dtos == null)
                return await base.BulkUpsertAsync(dtos!);

            var list = dtos as IList<PurchaseDto> ?? dtos.ToList();
            foreach (var dto in list)
                PurchaseCsvHelper.ApplyCalculatedFields(dto);

            return await base.BulkUpsertAsync(list);
        }
    }
}
