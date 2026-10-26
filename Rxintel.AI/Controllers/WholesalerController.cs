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
    public class WholesalerController : BaseController
    {
        private readonly IWholesalerService _wholesalerService;
        public WholesalerController(IWholesalerService wholesalerService)
        {
            _wholesalerService = wholesalerService;
        }
        [HttpGet]
        public async Task<PagedList<WholesalerDto>> GetAll([FromQuery] PageParam pageParam)
        {
            return await _wholesalerService.GetAll(pageParam);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<WholesalerDto>> GetByIdAsync(Guid id)
        {
            var result = await _wholesalerService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        [HttpPost("upsert")]
        public async Task<SuccessResult<WholesalerDto>> UpsertAsync([FromBody] WholesalerDto wholesalerDto)
        {
            return await _wholesalerService.UpsertAsync(wholesalerDto);
        }
        [HttpDelete("{id}")]
        public async Task<SuccessResult<WholesalerDto>> DeleteAsync(Guid id)
        {
            return await _wholesalerService.DeleteAsync(id);
        }
    }
}