using System.Globalization;
using System.Linq.Expressions;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services;

using FileFilter = Expression<Func<ImportErrorFile, bool>>;

public class ImportErrorService : IImportErrorService
{
    private const string DetailNotFoundMessage = "Import error detail not found.";
    private const string FileNotFoundMessage = "Import error record not found.";

    private static readonly Expression<Func<ImportErrorFile, object>>[] PharmacyInclude = [x => x.Pharmacy!];

    private readonly IImportErrorFileRepository _fileRepository;
    private readonly IImportErrorDetailRepository _detailRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenParser _tokenParser;

    public ImportErrorService(
        IImportErrorFileRepository fileRepository,
        IImportErrorDetailRepository detailRepository,
        IBillingRepository billingRepository,
        IPurchaseRepository purchaseRepository,
        IUnitOfWork unitOfWork,
        ITokenParser tokenParser)
    {
        _fileRepository = fileRepository;
        _detailRepository = detailRepository;
        _billingRepository = billingRepository;
        _purchaseRepository = purchaseRepository;
        _unitOfWork = unitOfWork;
        _tokenParser = tokenParser;
    }

    public async Task Save(
        ImportType importType,
        Guid pharmacyId,
        string fileName,
        IReadOnlyList<ImportErrorItemDto> errors)
    {
        await RegisterImportBatchAsync(
            importType,
            pharmacyId,
            fileName,
            _tokenParser.GetName() ?? _tokenParser.GetEmail(),
            errors);
    }

    public async Task<ImportErrorFileDto> RegisterImportBatchAsync(
        ImportType importType,
        Guid pharmacyId,
        string fileName,
        string? uploadedBy,
        IReadOnlyList<ImportErrorItemDto>? errors)
    {
        var normalizedFileName = ImportFileHelper.NormalizeStoredFileName(fileName);
        var failedRowCount = CountFailedImportRows(errors);
        var details = errors is { Count: > 0 } ? ImportErrorHelper.GroupIntoDetails(errors) : [];

        var file = new ImportErrorFile
        {
            ImportType = importType,
            PharmacyId = pharmacyId,
            FileName = normalizedFileName,
            ErrorCount = details.Count,
            FailedRowCount = failedRowCount,
            TotalImportedRows = 0,
            UploadedBy = string.IsNullOrWhiteSpace(uploadedBy) ? "Admin" : uploadedBy.Trim(),
            CreatedDate = DateTime.UtcNow,
            IsViewed = failedRowCount == 0,
            ViewedDate = failedRowCount == 0 ? DateTime.UtcNow : null
        };

        await _fileRepository.UpsertAsync(file, "Id");

        if (details.Count > 0)
        {
            foreach (var detail in details)
                detail.FileId = file.Id;

            await _detailRepository.BulkUpsertAsync(details, "Id");
        }

        return MapFileToDto(file);
    }

    public async Task UpdateTotalImportedRowsAsync(Guid fileId, int totalImportedRows)
    {
        var file = await _fileRepository.GetByIdAsync(fileId, "Id");
        if (file == null)
            return;

        file.TotalImportedRows = totalImportedRows;
        await _fileRepository.UpsertAsync(file, "Id");
    }

    public async Task<ImportErrorFileDto> GetOrCreateBillingSingleEntryFileAsync(Guid pharmacyId, string? uploadedBy)
    {
        var fileName = BuildSingleEntryFileName(DateTime.UtcNow);
        var existing = (await _fileRepository.GetAllAsync(
                x => x.ImportType == ImportType.Billing
                     && x.PharmacyId == pharmacyId
                     && x.FileName == fileName))
            .FirstOrDefault();

        if (existing != null)
            return MapFileToDto(existing);

        return await RegisterImportBatchAsync(
            ImportType.Billing,
            pharmacyId,
            fileName,
            uploadedBy,
            null);
    }

    public async Task<ImportErrorFileDto> GetOrCreatePurchaseSingleEntryFileAsync(Guid pharmacyId, string? uploadedBy)
    {
        var fileName = BuildSingleEntryFileName(DateTime.UtcNow);
        var existing = (await _fileRepository.GetAllAsync(
                x => x.ImportType == ImportType.Purchase
                     && x.PharmacyId == pharmacyId
                     && x.FileName == fileName))
            .FirstOrDefault();

        if (existing != null)
            return MapFileToDto(existing);

        return await RegisterImportBatchAsync(
            ImportType.Purchase,
            pharmacyId,
            fileName,
            uploadedBy,
            null);
    }

    public async Task<PagedList<ImportErrorFileDto>> GetFiles(ImportType importType, PageParam pageParam)
    {
        var dtos = (await _fileRepository.GetAllAsync(BuildFileFilter(importType), includes: PharmacyInclude))
            .ToList()
            .FilterBySearch(
                pageParam.SearchText,
                x => x.FileName,
                x => x.Pharmacy!.BusinessTradeName,
                x => x.UploadedBy ?? string.Empty)
            .OrderByDescending(x => x.CreatedDate)
            .Select(MapFileToDto)
            .ToList();

        return ToPagedList(dtos, pageParam);
    }

    public async Task<PagedList<ImportErrorDetailDto>> GetDetailsByFileId(Guid fileId, PageParam pageParam)
    {
        if (!await CanAccessFile(fileId))
            return EmptyDetailPage(pageParam);

        var dtos = (await _detailRepository.GetAllAsync(x => x.FileId == fileId))
            .Select(d => MapDetailToDto(d, includeMessages: true))
            .OrderBy(x => x.Row)
            .ThenBy(x => x.ErrorType)
            .ThenBy(x => x.NDC)
            .ThenBy(x => x.RxNoOrInvoice)
            .ToList();

        return ToPagedList(dtos, pageParam);
    }

    public async Task<SuccessResult<ImportErrorDetailDto>> GetDetailById(Guid detailId)
    {
        var detail = await _detailRepository.GetByIdAsync(detailId, "Id");
        if (detail == null || !await CanAccessFile(detail.FileId))
            return SuccessResult<ImportErrorDetailDto>.BadRequest(DetailNotFoundMessage);

        return SuccessResult<ImportErrorDetailDto>.Ok(MapDetailToDto(detail, includeMessages: true));
    }

    public Task<int> GetCount(ImportType importType) => GetUnreadCount(importType);

    public async Task<ImportErrorUnreadCountDto> GetUnreadCounts() => new()
    {
        BillingCount = await GetUnreadCount(ImportType.Billing),
        PurchaseCount = await GetUnreadCount(ImportType.Purchase)
    };

    public Task MarkAsViewed(ImportType importType)
    {
        var viewedDate = DateTime.UtcNow;
        return _fileRepository.UpdateAsync(
            BuildFileFilter(importType, unreadOnly: true),
            entity =>
            {
                entity.IsViewed = true;
                entity.ViewedDate = viewedDate;
            });
    }

    public async Task<SuccessResult<ImportErrorFileDto>> DeleteFile(Guid fileId)
    {
        var entity = await _fileRepository.GetByIdAsync(fileId, "Id", x => x.Pharmacy!);
        if (entity == null || !await CanAccessFile(fileId))
            return SuccessResult<ImportErrorFileDto>.BadRequest(FileNotFoundMessage);

        var billingRowsRemoved = 0;
        if (entity.ImportType == ImportType.Billing)
        {
            billingRowsRemoved = await _billingRepository.GetFilteredCountAsync(
                b => b.ImportErrorFileId == fileId);
        }

        var purchaseRowsRemoved = 0;
        if (entity.ImportType == ImportType.Purchase)
        {
            purchaseRowsRemoved = await _purchaseRepository.GetFilteredCountAsync(
                p => p.ImportErrorFileId == fileId);
        }

        var errorRecordsRemoved = await _detailRepository.GetFilteredCountAsync(d => d.FileId == fileId);

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (entity.ImportType == ImportType.Billing && billingRowsRemoved > 0)
            {
                await _billingRepository.ExecuteDeleteWhereAsync(b => b.ImportErrorFileId == fileId);
            }

            if (entity.ImportType == ImportType.Purchase && purchaseRowsRemoved > 0)
            {
                await _purchaseRepository.ExecuteDeleteWhereAsync(p => p.ImportErrorFileId == fileId);
            }

            if (errorRecordsRemoved > 0)
                await _detailRepository.ExecuteDeleteWhereAsync(d => d.FileId == fileId);

            var fileRowsDeleted = await _fileRepository.ExecuteDeleteWhereAsync(f => f.Id == fileId);
            if (fileRowsDeleted == 0)
            {
                await _unitOfWork.RollbackAsync(transaction);
                return SuccessResult<ImportErrorFileDto>.BadRequest(FileNotFoundMessage);
            }

            await _unitOfWork.CommitAsync(transaction);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(transaction);
            var inner = ex.InnerException?.Message ?? ex.Message;
            return SuccessResult<ImportErrorFileDto>.BadRequest(
                $"Could not delete import batch. {inner}");
        }

        var rowsRemoved = entity.ImportType == ImportType.Billing ? billingRowsRemoved : purchaseRowsRemoved;
        var message = BuildBatchDeleteMessage(entity.ImportType, rowsRemoved, errorRecordsRemoved);
        return SuccessResult<ImportErrorFileDto>.Ok(MapFileToDto(entity), message);
    }

    private static string BuildBatchDeleteMessage(
        ImportType importType,
        int rowsRemoved,
        int errorRecordsRemoved)
    {
        var parts = new List<string> { "Import batch deleted." };

        if (importType == ImportType.Billing && rowsRemoved > 0)
            parts.Add($"{rowsRemoved} billing row(s) removed.");
        else if (importType == ImportType.Purchase && rowsRemoved > 0)
            parts.Add($"{rowsRemoved} purchase row(s) removed.");

        if (errorRecordsRemoved > 0)
            parts.Add($"{errorRecordsRemoved} validation error record(s) removed.");

        return string.Join(" ", parts);
    }

    public async Task<SuccessResult<ImportErrorCsvDownloadDto>> DownloadErrorsCsv(Guid fileId)
    {
        if (!await CanAccessFile(fileId))
            return SuccessResult<ImportErrorCsvDownloadDto>.BadRequest(FileNotFoundMessage);

        var file = await _fileRepository.GetByIdAsync(fileId, "Id");
        if (file == null)
            return SuccessResult<ImportErrorCsvDownloadDto>.BadRequest(FileNotFoundMessage);

        var errors = (await _detailRepository.GetAllAsync(x => x.FileId == fileId))
            .SelectMany(d =>
            {
                var row = ImportErrorHelper.ParseRow(d.ErrorMessagesJson);
                return ImportErrorHelper.ParseData(d.ErrorMessagesJson)
                    .Select(field => new ImportErrorExportRowDto
                    {
                        Row = row,
                        NDC = d.NDC,
                        RxNoOrInvoice = d.RxNoOrInvoice,
                        FieldName = field.FieldName,
                        FieldValue = field.FieldValue,
                        ErrorMessage = field.ErrorMessage
                    });
            })
            .OrderBy(x => x.Row)
            .ThenBy(x => x.FieldName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var downloadFileName = ImportFileHelper.BuildErrorDownloadFileName(file.FileName);
        var isExcel = ImportFileHelper.IsExcelFile(file.FileName);

        return SuccessResult<ImportErrorCsvDownloadDto>.Ok(new ImportErrorCsvDownloadDto
        {
            Content = isExcel
                ? ImportErrorHelper.ToExcel(errors, file.FileName)
                : ImportErrorHelper.ToCsv(errors),
            FileName = downloadFileName
        });
    }

    private FileFilter BuildFileFilter(ImportType importType, bool unreadOnly = false)
    {
        var pharmacyId = _tokenParser.GetPharmacyId();

        if (pharmacyId.HasValue)
        {
            return unreadOnly
                ? x => x.ImportType == importType && x.PharmacyId == pharmacyId.Value && !x.IsViewed
                : x => x.ImportType == importType && x.PharmacyId == pharmacyId.Value;
        }

        return unreadOnly
            ? x => x.ImportType == importType && !x.IsViewed
            : x => x.ImportType == importType;
    }

    private async Task<bool> CanAccessFile(Guid fileId)
    {
        var pharmacyId = _tokenParser.GetPharmacyId();
        if (!pharmacyId.HasValue)
            return true;

        var file = await _fileRepository.GetByIdAsync(fileId, "Id");
        return file != null && file.PharmacyId == pharmacyId.Value;
    }

    private Task<int> GetUnreadCount(ImportType importType) =>
        _fileRepository.GetFilteredCountAsync(BuildFileFilter(importType, unreadOnly: true));

    private static PagedList<ImportErrorDetailDto> EmptyDetailPage(PageParam pageParam) =>
        new(new List<ImportErrorDetailDto>(), pageParam, 0);

    private static string BuildSingleEntryFileName(DateTime utcNow)
    {
        var month = utcNow.ToString("MMM", CultureInfo.InvariantCulture).ToUpperInvariant();
        return $"{month}{utcNow.Year}-SingalEntry.csv";
    }

    private static int CountFailedImportRows(IReadOnlyList<ImportErrorItemDto>? errors)
    {
        if (errors == null || errors.Count == 0)
            return 0;

        var distinctRows = errors.Where(e => e.Row > 0).Select(e => e.Row).Distinct().Count();
        return distinctRows > 0 ? distinctRows : errors.Count;
    }

    private static ImportErrorFileDto MapFileToDto(ImportErrorFile entity) => new()
    {
        Id = entity.Id,
        ImportType = entity.ImportType,
        PharmacyId = entity.PharmacyId,
        FileName = entity.FileName,
        PharmacyName = entity.Pharmacy?.BusinessTradeName ?? string.Empty,
        ErrorCount = entity.ErrorCount,
        FailedRowCount = entity.FailedRowCount,
        TotalImportedRows = entity.TotalImportedRows,
        UploadedBy = entity.UploadedBy ?? string.Empty,
        CreatedDate = entity.CreatedDate,
        IsViewed = entity.IsViewed,
        ViewedDate = entity.ViewedDate
    };

    private static ImportErrorDetailDto MapDetailToDto(ImportErrorDetail entity, bool includeMessages) => new()
    {
        Id = entity.Id,
        FileId = entity.FileId,
        Row = ImportErrorHelper.ParseRow(entity.ErrorMessagesJson),
        ErrorType = entity.ErrorType,
        NDC = entity.NDC,
        RxNoOrInvoice = entity.RxNoOrInvoice,
        Data = includeMessages
            ? ImportErrorHelper.ParseData(entity.ErrorMessagesJson)
            : new List<ImportErrorFieldDto>()
    };

    private static PagedList<T> ToPagedList<T>(List<T> items, PageParam pageParam) where T : class
    {
        var total = items.Count;
        var page = pageParam.Page is > 0 ? pageParam.Page.Value : 1;
        var pageSize = pageParam.PageSize is > 0 ? pageParam.PageSize.Value : 10;

        var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedList<T>(paged, pageParam, total);
    }
}
