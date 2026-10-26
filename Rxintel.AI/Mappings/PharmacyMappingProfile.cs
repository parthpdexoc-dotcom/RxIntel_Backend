using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;


namespace Rxintel.AI.Mappings
{
    public class PharmacyMappingProfile : Profile
    {
        public PharmacyMappingProfile()
        {
            CreateMap<Pharmacy, PharmacyDto>()
                .ForMember(d => d.StateName, opt => opt.MapFrom(s => s.State != null ? s.State.StateName : null));
            CreateMap<PharmacyDto, Pharmacy>()
                .ForMember(d => d.State, opt => opt.Ignore());
        }
    }
}
