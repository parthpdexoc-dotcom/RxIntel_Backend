using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.Generic.Services.Interfaces
{
    public interface IInsuranceService : IGenericService<InsuranceDto, Insurance>
    {
        Task<PagedList<InsuranceDto>> GetAll(PageParam pageParam, bool uniqueInsuranceNames = false);
        Task<List<InsuranceDto>> InsuranceDropdownById(Guid? Id = null);
    }
}
