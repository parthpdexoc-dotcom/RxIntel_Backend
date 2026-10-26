using Microsoft.AspNetCore.Http;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.Generic.Services.Interfaces
{
    public interface IBillingFieldMappingService : IGenericService<BillingFieldMappingDto, BillingFieldMapping>
    {
        Task<PagedList<BillingFieldMappingDto>> GetAll(PageParam pageParam, Guid? pharmacySoftwareId = null);
        Task<IEnumerable<BillingFieldMappingDto>> GetById(Guid? pharmacySoftwareId);
        Task<(BillingFieldMappingCsvPreviewDto? Preview, string? Error)> GetCsvPreviewFromUpload(IFormFile? file,Guid pharmacySoftwareId);
        Task<SuccessResult<IEnumerable<BillingFieldMappingDto>>> SaveMappingsForPharmacySoftware(IEnumerable<BillingFieldMappingDto> dtos);
    }
}
