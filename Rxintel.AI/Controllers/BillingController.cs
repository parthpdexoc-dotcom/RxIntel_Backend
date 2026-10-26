using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.AI.ControllerHelpers;
using Rxintel.Generic.Services.Interfaces;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
namespace Rxintel.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillingController : BaseController
    {
        private readonly IBillingService _billingService;
        public BillingController(IBillingService billingService)
        {
            _billingService = billingService;
        }
        [HttpGet]
        public async Task<PagedList<BillingDto>> GetAll(
            [FromQuery] PageParam pageParam,
            [FromQuery] Guid? importErrorFileId = null)
        {
            return await _billingService.GetAll(pageParam, importErrorFileId);
        }
        [HttpGet("columns")]
        public async Task<ActionResult<IEnumerable<string>>> GetBillingColumns()
        {
            var columns = await _billingService.GetBillingColumns();
            return Ok(columns);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<BillingDto>> GetById(Guid id)
        {
            var result = await _billingService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        /// <summary>Parse CSV (saved billing mappings) and bulk save billings.</summary>
        [HttpPost("bulk-upsert")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<SuccessResult<IEnumerable<BillingDto>>>> BulkUpsert(
            [FromForm] BillingCsvUploadRequest request)
        {
            var result = await _billingService.BulkUpsertFromCsvUpload(
                request.File,
                request.PharmacyId,
                request.PharmacySoftwareId);

            if (!result.IsSuccessStatusCode)
                return BadRequest(result);

            return Ok(result);
        }
        [HttpPost("upsert")]
        public async Task<SuccessResult<BillingDto>> Upsert([FromBody] BillingDto dto)
        {
            return await _billingService.UpsertAsync(dto);
        }
        [HttpDelete("{id}")]
        public async Task<SuccessResult<BillingDto>> DeleteById(Guid id)
        {
            return await _billingService.DeleteAsync(id);
        }
    }
}