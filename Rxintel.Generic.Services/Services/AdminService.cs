using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Models.Constants;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;
using System.Net;

namespace Rxintel.Generic.Services.Services
{
    public class AdminService : GenericService<AdminDto, Admin>, IAdminService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IWebHostEnvironment _environment;

        public AdminService(
            IAdminRepository adminRepository,
            IMapper mapper,
            ILogger<AdminService> logger,
            IWebHostEnvironment environment)
            : base(adminRepository, mapper, "Id")
        {
            _adminRepository = adminRepository;
            _environment = environment;
        }

        public override async Task<SuccessResult<AdminDto>> UpsertAsync(AdminDto adminDto)
        {
            if (adminDto != null)
            {
                adminDto.ProfileImage = string.IsNullOrWhiteSpace(adminDto.ProfileImage)
                    ? null
                    : adminDto.ProfileImage.Trim();

                if (adminDto.Id != Guid.Empty)
                {
                    var existing = await _adminRepository.GetByIdAsync(adminDto.Id, "Id");
                    if (existing != null)
                    {
                        if (string.IsNullOrWhiteSpace(adminDto.Password))
                            adminDto.Password = existing.Password;

                        if (string.IsNullOrWhiteSpace(adminDto.Role))
                            adminDto.Role = existing.Role;

                        adminDto.CreatedDate = existing.CreatedDate;

                        if (!string.IsNullOrWhiteSpace(existing.ProfileImage)
                            && !string.Equals(existing.ProfileImage, adminDto.ProfileImage, StringComparison.Ordinal))
                        {
                            ProfileImageValidationHelper.DeleteProfileImage(
                                _environment.ContentRootPath,
                                existing.ProfileImage);
                        }
                    }
                }
            }

            var result = await base.UpsertAsync(adminDto);

            if (result.Data != null)
            {
                result.Data.Password = null;
                result.Data.ConfirmPassword = null;
            }

            return result;
        }

        public async Task<SuccessResult<string>> UploadProfileImageAsync(IFormFile? file, Guid? userId = null)
        {
            if (!ProfileImageValidationHelper.IsAllowedImageFile(file))
            {
                var message = file != null && file.Length > ProfileImageValidationHelper.MaxImageSizeBytes
                    ? ModelValidationErrorCodes.INVALID_PHARMACY_USER_PROFILE_IMAGE_SIZE
                    : ModelValidationErrorCodes.INVALID_PHARMACY_USER_PROFILE_IMAGE_FORMAT;

                return new SuccessResult<string>
                {
                    Data = null,
                    Status = (int)HttpStatusCode.BadRequest,
                    Message = message
                };
            }

            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                var existing = await _adminRepository.GetByIdAsync(userId.Value, "Id");
                if (!string.IsNullOrWhiteSpace(existing?.ProfileImage))
                {
                    ProfileImageValidationHelper.DeleteProfileImage(
                        _environment.ContentRootPath,
                        existing.ProfileImage);
                }
            }

            var fileName = await ProfileImageValidationHelper.SaveProfileImageAsync(
                _environment.ContentRootPath,
                file!);

            return new SuccessResult<string>
            {
                Data = fileName,
                Status = (int)HttpStatusCode.OK,
                Message = "Profile image uploaded successfully"
            };
        }

        public string? GetProfileImagePhysicalPath(string fileName) =>
            ProfileImageValidationHelper.GetProfileImagePath(_environment.ContentRootPath, fileName);
    }
}
