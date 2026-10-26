using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Services.Interfaces;
using Rxintel.Generic.Dtos.Dtos;

namespace Rxintel.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : BaseController
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }
        
        [HttpGet]
        public async Task<IEnumerable<AdminDto>> GetAll()
        {
            return await _adminService.GetAllAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AdminDto>> GetById(Guid id)
        {
            var result = await _adminService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
       
        [HttpPost("upsert")]
        public async Task<SuccessResult<AdminDto>> Upsert([FromBody] AdminDto adminDto)
        {
            return await _adminService.UpsertAsync(adminDto);
        }

        [HttpPost("upload-profile-image")]
        [Consumes("multipart/form-data")]
        public async Task<SuccessResult<string>> UploadProfileImage(
            IFormFile file,
            [FromQuery] Guid? userId = null)
        {
            return await _adminService.UploadProfileImageAsync(file, userId);
        }

        [AllowAnonymous]
        [HttpGet("profile-image/{fileName}")]
        public IActionResult GetProfileImage(string fileName)
        {
            var path = _adminService.GetProfileImagePhysicalPath(fileName);
            if (path == null)
                return NotFound();

            return PhysicalFile(path, ProfileImageValidationHelper.GetContentType(Path.GetExtension(fileName)));
        }

        [HttpDelete("{id}")]
        public async Task<SuccessResult<AdminDto>> DeleteById(Guid id)
        {
            return await _adminService.DeleteAsync(id);
        }
    }
}
