using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.Generic.Services.Interfaces;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Common.Implementations.Responses;
namespace Rxintel.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InsuranceGroupController : BaseController
    {
        private readonly IInsuranceGroupService _insuranceGroupService;
        public InsuranceGroupController(IInsuranceGroupService insuranceGroupService)
        {
            _insuranceGroupService = insuranceGroupService;
        }           
        [HttpPost("save-mappings")] 
        public async Task<ActionResult<SuccessResult<List<InsuranceGroupDto>>>> SaveInsuranceMappings([FromBody] List<InsuranceGroupDto> insuranceGroupDtos)
        {
            var result = await _insuranceGroupService.SaveParentChildMappings(insuranceGroupDtos);
            return StatusCode(result.Status, result);
        }
        [HttpGet("selected-mappings/{binNumber}")]
        public async Task<ActionResult<List<InsuranceGroupDto>>> GetMappingsByBinNumber(string binNumber)
        {
            return await _insuranceGroupService.GetMappingsByBinNumber(binNumber);            
        }

        [HttpDelete("{id}")]
           public async Task<SuccessResult<InsuranceGroupDto>> DeleteAsync(Guid id)
        {
            return await _insuranceGroupService.DeleteAsync(id);
        }
    }
}
