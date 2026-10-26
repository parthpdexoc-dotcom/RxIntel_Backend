using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.Generic.Services.Interfaces
{
    public interface IInsuranceGroupService: IGenericService<InsuranceGroupDto, InsuranceGroup>
    {
        Task<SuccessResult<List<InsuranceGroupDto>>> SaveParentChildMappings(List<InsuranceGroupDto> insuranceGroupDtos);
        Task<List<InsuranceGroupDto>> GetMappingsByBinNumber(string binNumber);
    }
}
