using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.AI.Mappings
{
    public class PharmacyUserMappingProfile : Profile
    {
        public PharmacyUserMappingProfile()
        {
            CreateMap<PharmacyUser, PharmacyUserDto>()
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmPassword, opt => opt.Ignore());

            CreateMap<PharmacyUserDto, PharmacyUser>()
                .ForMember(dest => dest.Password, opt => opt.Ignore());
        }
    }
}
