using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.AI.Mappings
{
    public class StateMappingProfile : Profile
    {
        public StateMappingProfile()
        {
            CreateMap<State, StateDto>().ReverseMap();
        }
    }
}
