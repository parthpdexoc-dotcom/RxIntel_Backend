using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.AI.Mappings
{
    public class InsuranceMappingProfile : Profile
    {
        public InsuranceMappingProfile()
        {
            CreateMap<Insurance, InsuranceDto>().ReverseMap();
        }
    }
}
