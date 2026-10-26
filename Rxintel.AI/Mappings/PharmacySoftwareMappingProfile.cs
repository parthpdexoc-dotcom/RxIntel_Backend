using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.AI.Mappings
{
    public class PharmacySoftwareMappingProfile : Profile
    {
        public PharmacySoftwareMappingProfile()
        {
            CreateMap<PharmacyBillingSoftware, PharmacySoftwareDto>().ReverseMap();
        }
    }
}
