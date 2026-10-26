using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.AI.Mappings
{
    public class NDCGroupMappingProfile : Profile
    {
        public NDCGroupMappingProfile()
        {
            CreateMap<NDCGroup, NDCGroupDto>().ReverseMap();
        }
    }
}
