using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.AI.Mappings
{
    public class InsuranceGroupMappingProfile : Profile
    {
        public InsuranceGroupMappingProfile()
        {
            CreateMap<InsuranceGroup, InsuranceGroupDto>().ReverseMap();
        }
    }
}
