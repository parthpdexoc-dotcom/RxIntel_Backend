using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.AI.Mappings
{
    public class PurchaseMappingProfile : Profile
    {
        public PurchaseMappingProfile()
        {
            CreateMap<Purchase, PurchaseDto>().ReverseMap();
        }
    }
}
