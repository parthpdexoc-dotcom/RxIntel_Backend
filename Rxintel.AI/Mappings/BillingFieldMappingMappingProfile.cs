using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.AI.Mappings
{
    public class BillingFieldMappingMappingProfile : Profile
    {
        public BillingFieldMappingMappingProfile()
        {
            CreateMap<BillingFieldMapping, BillingFieldMappingDto>().ReverseMap();
        }
    }
}
