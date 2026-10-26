using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.AI.Mappings
{
    public class MedicineMappingProfile : Profile
    {
        public MedicineMappingProfile()
        {
            CreateMap<Medicine, MedicineDto>().ReverseMap();
        }
    }
}
