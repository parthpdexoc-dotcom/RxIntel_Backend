using AutoMapper;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Exceptions;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;
using System.Net;
namespace Rxintel.Generic.Services.Services
{
    public class NDCGroupService : GenericService<NDCGroupDto, NDCGroup>, INDCGroupService
    {
        private readonly INDCGroupRepository _ndcGroupRepository;
        public NDCGroupService(INDCGroupRepository repository, IMapper mapper, ILogger<NDCGroupService> logger) : base(repository, mapper, "Id")
        {
            _ndcGroupRepository = repository;
            _ = logger;
        }
        public async Task<SuccessResult<NDCGroupDto>> SaveParentChildMappings(NDCGroupDto ndcGroupDto)
        {
            if (ndcGroupDto == null || ndcGroupDto.ParentMedicineId == Guid.Empty)
            {
                return SuccessResult<NDCGroupDto>.BadRequest(CustomMessage.ParentRequiredMessage("ParentMedicineId"));
            }
            await _ndcGroupRepository.DeleteByPropertyAsync(nameof(NDCGroup.ParentMedicineId), ndcGroupDto.ParentMedicineId);
            var childIds = (ndcGroupDto.ChildMedicineIds ?? new List<Guid>()).Where(x => x != Guid.Empty && x != ndcGroupDto.ParentMedicineId).Distinct().ToList();
            if (childIds.Count == 0)
            {
                // Interpret empty child list as "remove all mappings for this parent"
                return BuildMappingSavedSuccess(ndcGroupDto.ParentMedicineId, new List<Guid>());
            }
            // create new mappings
            var mappings = childIds.Select(childId => new NDCGroup
            {
                Id = Guid.NewGuid(),ParentMedicineId = ndcGroupDto.ParentMedicineId,ChildMedicineId = childId}).ToList();
            await _ndcGroupRepository.AddRangeAsync(mappings);
            var savedMappings = await _ndcGroupRepository.GetAllAsync(x => x.ParentMedicineId == ndcGroupDto.ParentMedicineId);
            var savedChildMedicineIds = savedMappings.Select(x => x.ChildMedicineId).ToList();
            return BuildMappingSavedSuccess(ndcGroupDto.ParentMedicineId, savedChildMedicineIds);
        }
        public async Task<NDCGroupDto> GetMappingsByParentId(Guid parentMedicineId)
        {
            if (parentMedicineId == Guid.Empty)
            {
                throw new WebApiException(HttpStatusCode.BadRequest, CustomMessage.ParentRequiredMessage("ParentMedicineId"));
            }
            var mappings = await _ndcGroupRepository.GetAllAsync(x => x.ParentMedicineId == parentMedicineId);
            var childMedicineIds = mappings.Select(x => x.ChildMedicineId).ToList();
            return new NDCGroupDto
            {
                ParentMedicineId = parentMedicineId,
                ChildMedicineIds = childMedicineIds
            };
        }

        private static SuccessResult<NDCGroupDto> BuildMappingSavedSuccess(Guid parentMedicineId, List<Guid> childMedicineIds)
        {
            return new SuccessResult<NDCGroupDto>
            {
                Status = (int)HttpStatusCode.OK,
                Message = CustomMessage.GetMappingSavedMessage(nameof(NDCGroup)),
                Data = new NDCGroupDto
                {
                    ParentMedicineId = parentMedicineId,
                    ChildMedicineIds = childMedicineIds
                }
            };
        }
    }
}
