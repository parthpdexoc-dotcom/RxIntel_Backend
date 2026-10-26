using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IPharmacySoftwareService : IGenericService<PharmacySoftwareDto, PharmacyBillingSoftware>
    {
        Task<PagedList<PharmacySoftwareDto>> GetAll(PageParam pageParam);
    }
}
