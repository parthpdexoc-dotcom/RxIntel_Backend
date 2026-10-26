using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.AI.Mappings
{
    public class WholesalerFieldMappingMappingProfile : Profile
    {
        public WholesalerFieldMappingMappingProfile()
        {
            CreateMap<WholesalerFieldMapping, WholesalerFieldMappingDto>().ReverseMap();
        }
    }
}
