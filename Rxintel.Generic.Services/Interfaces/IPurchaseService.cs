using Microsoft.AspNetCore.Http;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.Generic.Services.Interfaces
{
    public interface IPurchaseService : IGenericService<PurchaseDto, Purchase>
    {
        Task<PagedList<PurchaseDto>> GetAll(PageParam pageParam, Guid? importErrorFileId = null);
        Task<IEnumerable<string>> GetPurchaseColumns();
        Task<(IReadOnlyList<PurchaseDto> Rows, IReadOnlyList<ImportErrorItemDto>? Errors)> ParseCsvFromUpload(
            IFormFile? file,
            Guid pharmacyId,
            Guid wholesalerId);

        Task<SuccessResult<IEnumerable<PurchaseDto>>> BulkUpsertFromCsvUpload(
            IFormFile? file,
            Guid pharmacyId,
            Guid wholesalerId);
    }
}
