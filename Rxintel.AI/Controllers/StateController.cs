using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.AI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class StateController : BaseController
    {
        private readonly IStateService _stateService;
       
        public StateController(IStateService stateService)
        {
            _stateService = stateService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IEnumerable<StateDto>> GetAllAsync()
        {
            return await _stateService.GetAllAsync();
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<StateDto>> GetByIdAsync(Guid id)
        {
            var result = await _stateService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        /// <summary>
        /// Insert or Update state
        /// </summary>
        [HttpPost("upsert")]
        public async Task<SuccessResult<StateDto>> UpsertAsync([FromBody] StateDto stateDto)
        {
            return await _stateService.UpsertAsync(stateDto);
        }

        /// <summary>
        /// Delete state by Id
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<SuccessResult<StateDto>> DeleteAsync(Guid id)
        {
            return await _stateService.DeleteAsync(id);
        }
    }
}