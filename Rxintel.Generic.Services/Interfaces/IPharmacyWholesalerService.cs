using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IPharmacyWholesalerService : IGenericService<PharmacyWholesalerDto, PharmacyWholesaler>
    {
        Task<PagedList<PharmacyWholesalerDto>> GetAll(PageParam pageParam, Guid? pharmacyId = null);
    }
}
