using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.Generic.Services.Interfaces;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.AI.ControllerHelpers;
namespace Rxintel.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseController : BaseController
    {
        private readonly IPurchaseService _purchaseService;
        public PurchaseController(IPurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }
        [HttpGet]
        public async Task<PagedList<PurchaseDto>> GetAll(
            [FromQuery] PageParam pageParam,
            [FromQuery] Guid? importErrorFileId = null)
        {
            return await _purchaseService.GetAll(pageParam, importErrorFileId);
        }
       

        /// <summary>Parse CSV (saved wholesaler mappings) and bulk save purchases.</summary>
        [HttpPost("bulk-upsert")]
        [Consumes("multipart/form-data")]       
       public async Task<ActionResult<SuccessResult<IEnumerable<PurchaseDto>>>> BulkUpsertAsync(
          [FromForm] PurchaseCsvUploadRequest request)
       
        {
            var result = await _purchaseService.BulkUpsertFromCsvUpload(
                request.File,
                request.PharmacyId,
                request.WholesalerId);

            if (!result.IsSuccessStatusCode)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PurchaseDto>> GetByIdAsync(Guid id)
        {
            var result = await _purchaseService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost("upsert")]
        public async Task<SuccessResult<PurchaseDto>> UpsertAsync([FromBody] PurchaseDto dto)
        {
            return await _purchaseService.UpsertAsync(dto);
        }

        [HttpDelete("{id:guid}")]
        public async Task<SuccessResult<PurchaseDto>> DeleteAsync(Guid id)
        {
            return await _purchaseService.DeleteAsync(id);
        }
    }
}