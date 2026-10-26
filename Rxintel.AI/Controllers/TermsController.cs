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
    public class TermsController : BaseController
    {
        private readonly ITermsService _termsService;

        public TermsController(ITermsService termsService)
        {
            _termsService = termsService;
        }

        [AllowAnonymous]
        [HttpGet("current")]
        [ProducesResponseType(typeof(TermsAndConditionsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TermsAndConditionsDto>> GetCurrentAsync()
        {
            var terms = await _termsService.GetCurrentAsync();
            if (terms == null)
                return NotFound();

            return Ok(terms);
        }

        [HttpPut("current")]
        public async Task<SuccessResult<TermsAndConditionsDto>> UpsertCurrentAsync(
            [FromBody] TermsAndConditionsUpdateDto dto)
        {
            return await _termsService.UpsertCurrentAsync(dto);
        }
    }
}
