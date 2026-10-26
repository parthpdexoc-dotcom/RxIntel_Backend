using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.Generic.Services.Interfaces;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
namespace Rxintel.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InsuranceController : BaseController
    {
        private readonly IInsuranceService _insuranceService;
        public InsuranceController(IInsuranceService insuranceService)
        {
            _insuranceService = insuranceService;
        }
        [HttpGet]
        public async Task<PagedList<InsuranceDto>> GetAll(
            [FromQuery] PageParam pageParam,
            [FromQuery] bool uniqueInsuranceNames = false)
        {
            return await _insuranceService.GetAll(pageParam, uniqueInsuranceNames);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<InsuranceDto>> GetByIdAsync(Guid id)
        {
            var result = await _insuranceService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        [HttpGet("dropdown")]
        public async Task<ActionResult<List<InsuranceDto>>> InsuranceDropdownById([FromQuery] Guid? Id = null)
        {        
            var data = await _insuranceService.InsuranceDropdownById(Id);
            return Ok(data);
        }
        [HttpPost("upsert")]
        public async Task<SuccessResult<InsuranceDto>> UpsertAsync([FromBody] InsuranceDto insuranceDto)
        {
            return await _insuranceService.UpsertAsync(insuranceDto);
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult<SuccessResult<InsuranceDto>>> DeleteAsync(Guid id)
        {
            var result = await _insuranceService.DeleteAsync(id);
            if (!result.IsSuccessStatusCode)
                return BadRequest(result);
            return Ok(result);
        }
    }
}