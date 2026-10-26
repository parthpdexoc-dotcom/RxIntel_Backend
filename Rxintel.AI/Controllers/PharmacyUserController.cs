using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public class PharmacyUserController : BaseController
    {
        private readonly IPharmacyUserService _pharmacyUserService;

        public PharmacyUserController(IPharmacyUserService pharmacyUserService)
        {
            _pharmacyUserService = pharmacyUserService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<PagedList<PharmacyUserDto>> GetAll([FromQuery] PageParam pageParam, [FromQuery] Guid? pharmacyId)
        {
            return await _pharmacyUserService.GetAll(pageParam, pharmacyId);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PharmacyUserDto>> GetByIdAsync(Guid id)
        {
            var result = await _pharmacyUserService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost("upsert")]
        public async Task<SuccessResult<PharmacyUserDto>> UpsertAsync([FromBody] PharmacyUserDto pharmacyUserDto)
        {
            return await _pharmacyUserService.UpsertAsync(pharmacyUserDto);
        }

        [HttpPost("upload-profile-image")]
        [Consumes("multipart/form-data")]
        public async Task<SuccessResult<string>> UploadProfileImage(
            IFormFile file,
            [FromQuery] Guid? userId = null)
        {
            return await _pharmacyUserService.UploadProfileImageAsync(file, userId);
        }

        [AllowAnonymous]
        [HttpGet("profile-image/{fileName}")]
        public IActionResult GetProfileImage(string fileName)
        {
            var path = _pharmacyUserService.GetProfileImagePhysicalPath(fileName);
            if (path == null)
                return NotFound();

            return PhysicalFile(path, ProfileImageValidationHelper.GetContentType(Path.GetExtension(fileName)));
        }

        [HttpDelete("{id}")]
        public async Task<SuccessResult<PharmacyUserDto>> DeleteAsync(Guid id)
        {
            return await _pharmacyUserService.DeleteAsync(id);
        }
    }
}
