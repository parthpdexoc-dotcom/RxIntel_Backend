using AutoMapper;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.AI.Mappings
{
    public class WholesalerMappingProfile : Profile
    {
        public WholesalerMappingProfile()
        {
            CreateMap<Wholesaler, WholesalerDto>();
            CreateMap<WholesalerDto, Wholesaler>()
                .ForMember(
                    dest => dest.VawdDate,
                    opt => opt.MapFrom(src => WholesalerVawdExpiryHelper.NormalizeVawdDate(src.Vawd, src.VawdDate)))
                .ForMember(
                    dest => dest.ExpiryDate,
                    opt => opt.MapFrom(src => WholesalerVawdExpiryHelper.FromVawd(src.Vawd, src.VawdDate)));
        }
    }
}
