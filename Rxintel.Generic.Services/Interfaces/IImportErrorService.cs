using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Services.Interfaces;

public interface IImportErrorService
{
    Task Save(ImportType importType,Guid pharmacyId,string fileName,IReadOnlyList<ImportErrorItemDto> errors);

    Task<ImportErrorFileDto> RegisterImportBatchAsync(
        ImportType importType,
        Guid pharmacyId,
        string fileName,
        string? uploadedBy,
        IReadOnlyList<ImportErrorItemDto>? errors);

    Task UpdateTotalImportedRowsAsync(Guid fileId, int totalImportedRows);

    Task<ImportErrorFileDto> GetOrCreateBillingSingleEntryFileAsync(Guid pharmacyId, string? uploadedBy);
    Task<ImportErrorFileDto> GetOrCreatePurchaseSingleEntryFileAsync(Guid pharmacyId, string? uploadedBy);

    Task<PagedList<ImportErrorFileDto>> GetFiles(ImportType importType, PageParam pageParam);

    Task<PagedList<ImportErrorDetailDto>> GetDetailsByFileId(Guid fileId, PageParam pageParam);

    Task<SuccessResult<ImportErrorDetailDto>> GetDetailById(Guid detailId);

    Task<int> GetCount(ImportType importType);

    Task<ImportErrorUnreadCountDto> GetUnreadCounts();

    Task MarkAsViewed(ImportType importType);

    Task<SuccessResult<ImportErrorFileDto>> DeleteFile(Guid fileId);

    Task<SuccessResult<ImportErrorCsvDownloadDto>> DownloadErrorsCsv(Guid fileId);
}
