using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.AI.Mappings
{
    public class PharmacyWholesalerMappingProfile : Profile
    {
        public PharmacyWholesalerMappingProfile()
        {
            CreateMap<PharmacyWholesaler, PharmacyWholesalerDto>().ReverseMap();
        }
    }
}
