using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.Generic.Services.Interfaces
{
    public interface INDCGroupService : IGenericService<NDCGroupDto, NDCGroup>
    {
        Task<SuccessResult<NDCGroupDto>> SaveParentChildMappings(NDCGroupDto ndcGroupDto);
        Task<NDCGroupDto> GetMappingsByParentId(Guid parentNDCId);
    }
}