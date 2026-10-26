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
    public class PharmacyWholesalerController : BaseController
    {
        private readonly IPharmacyWholesalerService _pharmacyWholesalerService;

        public PharmacyWholesalerController(IPharmacyWholesalerService pharmacyWholesalerService)
        {
            _pharmacyWholesalerService = pharmacyWholesalerService;
        }

        [HttpGet]
        public async Task<PagedList<PharmacyWholesalerDto>> GetAll([FromQuery] PageParam pageParam, [FromQuery] Guid? pharmacyId)
        {
            return await _pharmacyWholesalerService.GetAll(pageParam, pharmacyId);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PharmacyWholesalerDto>> GetByIdAsync(Guid id)
        {
            var result = await _pharmacyWholesalerService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost("upsert")]
        public async Task<SuccessResult<PharmacyWholesalerDto>> UpsertAsync([FromBody] PharmacyWholesalerDto pharmacyWholesalerDto)
        {
            return await _pharmacyWholesalerService.UpsertAsync(pharmacyWholesalerDto);
        }

        [HttpDelete("{id}")]
        public async Task<SuccessResult<PharmacyWholesalerDto>> DeleteAsync(Guid id)
        {
            return await _pharmacyWholesalerService.DeleteAsync(id);
        }
    }
}
