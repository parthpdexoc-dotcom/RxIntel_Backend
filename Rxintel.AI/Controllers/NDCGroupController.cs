using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.Generic.Services.Interfaces;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Common.Implementations.Responses;
namespace Rxintel.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NDCGroupController : BaseController
    {
        private readonly INDCGroupService _ndcGroupService;
        public NDCGroupController(INDCGroupService ndcGroupService)
        {
            _ndcGroupService = ndcGroupService;
        }               
        [HttpPost("save-mappings")]
        public async Task<SuccessResult<NDCGroupDto>> SaveMedicineMappings([FromBody] NDCGroupDto ndcGroupDto)
        {
            return await _ndcGroupService.SaveParentChildMappings(ndcGroupDto);
        }
        [HttpGet("selected-mappings/{parentMedicineId}")]
        public async Task<ActionResult<NDCGroupDto>> GetMappingsByParentMedicineId(Guid parentMedicineId)
        {
            return await _ndcGroupService.GetMappingsByParentId(parentMedicineId);            
        }
    }
}