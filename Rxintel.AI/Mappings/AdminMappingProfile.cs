using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;


namespace Rxintel.AI.Mappings
{
       public class AdminMappingProfile : Profile
    {
        public AdminMappingProfile()
        {
            CreateMap<Admin, AdminDto>().ReverseMap();
        }
    }
}
