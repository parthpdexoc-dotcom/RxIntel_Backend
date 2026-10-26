using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.AI.ControllerHelpers;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Services.Interfaces;
namespace Rxintel.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillingFieldMappingController : BaseController
    {
        private readonly IBillingFieldMappingService _billingFieldMappingService;
        public BillingFieldMappingController(IBillingFieldMappingService billingFieldMappingService)
        {
            _billingFieldMappingService = billingFieldMappingService;
        }
        [HttpGet]
        public async Task<PagedList<BillingFieldMappingDto>> GetAll([FromQuery] PageParam? pageParam, [FromQuery] Guid? pharmacySoftwareId)
        {
            return await _billingFieldMappingService.GetAll(pageParam, pharmacySoftwareId);
        }

        [HttpGet("by-pharmacy-software")]
        public async Task<IEnumerable<BillingFieldMappingDto>> GetById([FromQuery] Guid? pharmacySoftwareId)
        {
            return await _billingFieldMappingService.GetById(pharmacySoftwareId);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BillingFieldMappingDto>> GetById(Guid id)
        {
            var result = await _billingFieldMappingService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        [HttpPost("upsert")]
        public async Task<SuccessResult<BillingFieldMappingDto>> Upsert([FromBody] BillingFieldMappingDto billingFieldMappingDto)
        {
            return await _billingFieldMappingService.UpsertAsync(billingFieldMappingDto);
        }

        [HttpPost("upload-csv-preview")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<BillingFieldMappingCsvPreviewDto>> UploadCsvPreviewAsync(
            [FromForm] BillingFieldMappingCsvUploadRequest request)
        {
            var (preview, error) = await _billingFieldMappingService.GetCsvPreviewFromUpload(
                request.File,
                request.PharmacySoftwareId);

            if (error != null)
                return BadRequest(error);

            return Ok(preview);
        }

        [HttpPost("bulk-upsert")]
        public async Task<SuccessResult<IEnumerable<BillingFieldMappingDto>>> BulkUpsertAsync(
            [FromBody] List<BillingFieldMappingDto> billingFieldMappingDtos)
        {
            return await _billingFieldMappingService.SaveMappingsForPharmacySoftware(billingFieldMappingDtos);
        }
        [HttpDelete("{id}")]
        public async Task<SuccessResult<BillingFieldMappingDto>> DeleteById(Guid id)
        {
            return await _billingFieldMappingService.DeleteAsync(id);
        }
    }
}