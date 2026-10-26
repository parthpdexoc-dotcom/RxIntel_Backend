using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PharmacySoftwareController : BaseController
    {
        private readonly IPharmacySoftwareService _pharmacySoftwareService;

        public PharmacySoftwareController(IPharmacySoftwareService pharmacySoftwareService)
        {
            _pharmacySoftwareService = pharmacySoftwareService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<PagedList<PharmacySoftwareDto>> GetAll([FromQuery] PageParam pageParam)
        {
            return await _pharmacySoftwareService.GetAll(pageParam);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<PharmacySoftwareDto>> GetByIdAsync(Guid id)
        {
            var result = await _pharmacySoftwareService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost("upsert")]
        public async Task<SuccessResult<PharmacySoftwareDto>> UpsertAsync([FromBody] PharmacySoftwareDto pharmacySoftwareDto)
        {
            return await _pharmacySoftwareService.UpsertAsync(pharmacySoftwareDto);
        }

        [HttpDelete("{id}")]
        public async Task<SuccessResult<PharmacySoftwareDto>> DeleteAsync(Guid id)
        {
            return await _pharmacySoftwareService.DeleteAsync(id);
        }
    }
}
