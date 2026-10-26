using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.AI.Mappings
{
    public class BillingMappingProfile : Profile
    {
        public BillingMappingProfile()
        {
            CreateMap<Billing, BillingDto>().ReverseMap();
        }
    }
}
