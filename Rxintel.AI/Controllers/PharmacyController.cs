using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Common.Exceptions;
using Rxintel.Generic.Api;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Services.Interfaces;
namespace Rxintel.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PharmacyController : BaseController
    {
        private readonly IPharmacyService _pharmacyService;

        public PharmacyController(IPharmacyService pharmacyService)
        {
            _pharmacyService = pharmacyService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TokenDto>> Login([FromBody] AdminDto adminDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tokenDto = await _pharmacyService.Login(
                    adminDto.Email ?? string.Empty,
                    adminDto.Password ?? string.Empty);
                return Ok(tokenDto);
            }
            catch (WebApiException ex)
            {
                return StatusCode((int)ex.HttpStatusCode, ex.ErrorMessage);
            }
        }

        [AllowAnonymous]
        [HttpPost("register/pharmacy")]
        [Consumes("multipart/form-data")]
        public async Task<SuccessResult<PharmacyDto>> RegisterPharmacy(
            [FromForm] PharmacyDto pharmacyDto,
            IFormFile? deaDocument,
            IFormFile? stateBoardLicenseDocument)
        {
            return await _pharmacyService.RegisterPharmacy(
                pharmacyDto,
                deaDocument,
                stateBoardLicenseDocument);
        }

        [HttpGet]
        public async Task<PagedList<PharmacyDto>> GetAll([FromQuery] PageParam pageParam)
        {
            return await _pharmacyService.GetAll(pageParam);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<PharmacyDto>> GetById(Guid id)
        {
            var result = await _pharmacyService.GetByIdAsync(id);

            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpGet("{id}/dea-document/view")]
        public async Task<IActionResult> ViewDeaDocument(Guid id)
        {
            var path = await _pharmacyService.GetDeaDocumentPhysicalPathAsync(id);
            if (path == null)
                return NotFound();

            return PhysicalFile(path, PharmacyDocumentStorageHelper.GetContentType(Path.GetExtension(path)));
        }

        [HttpGet("{id}/dea-document/download")]
        public async Task<IActionResult> DownloadDeaDocument(Guid id)
        {
            var path = await _pharmacyService.GetDeaDocumentPhysicalPathAsync(id);
            if (path == null)
                return NotFound();

            return PhysicalFile(
                path,
                PharmacyDocumentStorageHelper.GetContentType(Path.GetExtension(path)),
                Path.GetFileName(path));
        }

        [HttpGet("{id}/state-board-license/view")]
        public async Task<IActionResult> ViewStateBoardLicense(Guid id)
        {
            var path = await _pharmacyService.GetStateBoardLicenseDocumentPhysicalPathAsync(id);
            if (path == null)
                return NotFound();

            return PhysicalFile(path, PharmacyDocumentStorageHelper.GetContentType(Path.GetExtension(path)));
        }

        [HttpGet("{id}/state-board-license/download")]
        public async Task<IActionResult> DownloadStateBoardLicense(Guid id)
        {
            var path = await _pharmacyService.GetStateBoardLicenseDocumentPhysicalPathAsync(id);
            if (path == null)
                return NotFound();

            return PhysicalFile(
                path,
                PharmacyDocumentStorageHelper.GetContentType(Path.GetExtension(path)),
                Path.GetFileName(path));
        }

        [HttpPost("upsert")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<SuccessResult<PharmacyDto>>> Upsert(
            [FromForm] PharmacyDto pharmacyDto,
            IFormFile? deaDocument,
            IFormFile? stateBoardLicenseDocument)
        {
            var result = await _pharmacyService.UpsertPharmacy(
                pharmacyDto,
                deaDocument,
                stateBoardLicenseDocument);
            if (!result.IsSuccessStatusCode)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult<SuccessResult<PharmacyDto>>> Delete(Guid id)
        {
            var result = await _pharmacyService.DeleteAsync(id);
            if (!result.IsSuccessStatusCode)
                return BadRequest(result);
            return Ok(result);
        }

    }
}