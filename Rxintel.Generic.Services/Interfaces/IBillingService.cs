using Microsoft.AspNetCore.Http;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.Generic.Services.Interfaces
{
    public interface IBillingService : IGenericService<BillingDto, Billing>
    {
        Task<PagedList<BillingDto>> GetAll(PageParam pageParam, Guid? importErrorFileId = null);
        Task<IEnumerable<string>> GetBillingColumns();
        Task<(IReadOnlyList<BillingDto> Rows, IReadOnlyList<ImportErrorItemDto>? Errors)> ParseCsvFromUpload(
            IFormFile? file,
            Guid pharmacyId,
            Guid pharmacySoftwareId);

        Task<SuccessResult<IEnumerable<BillingDto>>> BulkUpsertFromCsvUpload(
            IFormFile? file,
            Guid pharmacyId,
            Guid pharmacySoftwareId);
    }
}
