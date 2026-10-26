using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.Generic.Services.Interfaces
{
    public interface IWholesalerService : IGenericService<WholesalerDto, Wholesaler>
    {
        Task<PagedList<WholesalerDto>> GetAll(PageParam pageParam);
    }
}
