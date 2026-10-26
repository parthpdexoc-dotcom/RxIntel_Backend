using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.AI.Mappings
{
    public class TermsMappingProfile : Profile
    {
        public TermsMappingProfile()
        {
            CreateMap<TermsAndConditions, TermsAndConditionsDto>();
        }
    }
}
