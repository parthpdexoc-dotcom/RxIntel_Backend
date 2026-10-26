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
    public class WholesalerFieldMappingController : BaseController
    {
        private readonly IWholesalerFieldMappingService _wholesalerFieldMappingService;
        public WholesalerFieldMappingController(IWholesalerFieldMappingService wholesalerFieldMappingService)
        {
            _wholesalerFieldMappingService = wholesalerFieldMappingService;
        }
        [HttpGet]
        public async Task<PagedList<WholesalerFieldMappingDto>> GetAll([FromQuery] PageParam pageParam,[FromQuery] Guid? wholesalerId)
        {
            return await _wholesalerFieldMappingService.GetAll(pageParam, wholesalerId);
        }

        [HttpGet("by-wholesaler")]
        public async Task<IEnumerable<WholesalerFieldMappingDto>> GetByWholesalerId([FromQuery] Guid? wholesalerId)
        {
            return await _wholesalerFieldMappingService.GetByWholesalerId(wholesalerId);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WholesalerFieldMappingDto>> GetByIdAsync(Guid id)
        {
            var result = await _wholesalerFieldMappingService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        [HttpPost("upsert")]
        public async Task<SuccessResult<WholesalerFieldMappingDto>> UpsertAsync([FromBody] WholesalerFieldMappingDto wholesalerFieldMappingDto)
        {
            return await _wholesalerFieldMappingService.UpsertAsync(wholesalerFieldMappingDto);
        }

        [HttpPost("upload-csv-preview")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<WholesalerFieldMappingCsvPreviewDto>> UploadCsvPreviewAsync(
            [FromForm] WholesalerFieldMappingCsvUploadRequest request)
        {
            var (preview, error) = await _wholesalerFieldMappingService.GetCsvPreviewFromUpload(
                request.File,
                request.WholesalerId);

            if (error != null)
                return BadRequest(error);

            return Ok(preview);
        }

        [HttpPost("bulk-upsert")]
        public async Task<SuccessResult<IEnumerable<WholesalerFieldMappingDto>>> BulkUpsertAsync(
            [FromBody] List<WholesalerFieldMappingDto> wholesalerFieldMappingDtos)
        {
            return await _wholesalerFieldMappingService.BulkUpsertByWholesaler(wholesalerFieldMappingDtos);
        }
        [HttpDelete("{id}")]
        public async Task<SuccessResult<WholesalerFieldMappingDto>> DeleteAsync(Guid id)
        {
            return await _wholesalerFieldMappingService.DeleteAsync(id);
        }
    }
}
