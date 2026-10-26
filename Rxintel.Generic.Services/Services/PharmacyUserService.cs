using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Models.Constants;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Helpers;
using Rxintel.Generic.Services.Interfaces;
using System.Linq.Expressions;
using System.Net;

namespace Rxintel.Generic.Services.Services
{
    public class PharmacyUserService : GenericService<PharmacyUserDto, PharmacyUser>, IPharmacyUserService
    {
        private readonly IPharmacyUserRepository _pharmacyUserRepository;
        private readonly IPharmacyRepository _pharmacyRepository;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment;

        public PharmacyUserService(IPharmacyUserRepository pharmacyUserRepository,IPharmacyRepository pharmacyRepository,IMapper mapper,IWebHostEnvironment environment) : base(pharmacyUserRepository, mapper, "Id")
        {
            _pharmacyUserRepository = pharmacyUserRepository;
            _pharmacyRepository = pharmacyRepository;
            _mapper = mapper;
            _environment = environment;
        }

        public async Task<PagedList<PharmacyUserDto>> GetAll(PageParam pageParam, Guid? pharmacyId = null)
        {
            Expression<Func<PharmacyUser, bool>>? filter = null;

            if (pharmacyId.HasValue && pageParam?.IsActive.HasValue == true)
                filter = x => x.PharmacyId == pharmacyId.Value && x.IsActive == pageParam.IsActive.Value;
            else if (pharmacyId.HasValue)
                filter = x => x.PharmacyId == pharmacyId.Value;
            else if (pageParam?.IsActive.HasValue == true)
                filter = x => x.IsActive == pageParam.IsActive.Value;

            var hasSearch = !string.IsNullOrWhiteSpace(pageParam?.SearchText);

            if (!hasSearch && pageParam != null && pageParam.IsPaginationRequested)
            {
                var totalRecords = await _pharmacyUserRepository.GetFilteredCountAsync(filter);
                var pageEntities = (await _pharmacyUserRepository.GetAllAsync(filter, null, pageParam)).ToList();
                var pageData = _mapper.Map<List<PharmacyUserDto>>(pageEntities);
                return new PagedList<PharmacyUserDto>(pageData, pageParam, totalRecords);
            }

            var data = (await GetAllAsync(filter)).ToList();
            data = data.FilterBySearch(
                pageParam?.SearchText,
                p => p.Name,
                p => p.UserEmail,
                p => p.Role.HasValue ? EnumHelper.ToStringValue(p.Role.Value) : string.Empty
              );
            return ToPagedList(data, pageParam);
        }

        public override async Task<SuccessResult<PharmacyUserDto>> UpsertAsync(PharmacyUserDto pharmacyUserDto)
        {
            if (pharmacyUserDto == null)
                return await base.UpsertAsync(pharmacyUserDto);

            pharmacyUserDto.ProfileImage = string.IsNullOrWhiteSpace(pharmacyUserDto.ProfileImage)
                ? null
                : pharmacyUserDto.ProfileImage.Trim();

            var isNew = pharmacyUserDto.Id == Guid.Empty;
            PharmacyUser? existing = null;

            if (!isNew)
            {
                existing = await _pharmacyUserRepository.GetByIdAsync(pharmacyUserDto.Id, "Id");
                if (existing != null)
                {
                    pharmacyUserDto.CreatedDate = existing.CreatedDate;

                    if (!string.IsNullOrWhiteSpace(existing.ProfileImage)
                        && !string.Equals(existing.ProfileImage, pharmacyUserDto.ProfileImage, StringComparison.Ordinal))
                    {
                        ProfileImageValidationHelper.DeleteProfileImage(
                            _environment.ContentRootPath,
                            existing.ProfileImage);
                    }
                }
            }

            var entity = _mapper.Map<PharmacyUser>(pharmacyUserDto);

            if (isNew)
            {
                entity.Password = PharmacyUserPasswordHelper.HashPassword(
                    entity,
                    pharmacyUserDto.Password!);
            }
            else if (existing != null)
            {
                entity.Password = existing.Password;
            }

            await _pharmacyUserRepository.UpsertAsync(entity, "Id");
            var resultDto = _mapper.Map<PharmacyUserDto>(entity);
            ClearPasswordFields(resultDto);

            return new SuccessResult<PharmacyUserDto>
            {
                Data = resultDto,
                Status = (int)HttpStatusCode.OK,
                Message = isNew
                    ? CustomMessage.GetSaveMessage(nameof(PharmacyUser))
                    : CustomMessage.GetUpdateMessage(nameof(PharmacyUser))
            };
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
                var existing = await _pharmacyUserRepository.GetByIdAsync(userId.Value, "Id");
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

        public async Task<SuccessResult<PharmacyUserDto>> RegisterPharmacyUser(PharmacyUserDto pharmacyUserDto)
        {
            if (pharmacyUserDto == null)
                return BadRegister<PharmacyUserDto>(CustomMessage.GetInvalidMessage("Registration"));

            if (pharmacyUserDto.PharmacyId == Guid.Empty ||
                string.IsNullOrWhiteSpace(pharmacyUserDto.UserEmail) ||
                string.IsNullOrWhiteSpace(pharmacyUserDto.Password) ||
                string.IsNullOrWhiteSpace(pharmacyUserDto.Name))
            {
                return BadRegister<PharmacyUserDto>(CustomMessage.EmployeeRegistrationFieldsRequired);
            }

            var pharmacies = await _pharmacyRepository.GetAllAsync(p => p.Id == pharmacyUserDto.PharmacyId);
            var pharmacy = pharmacies.FirstOrDefault();

            if (pharmacy == null)
                return BadRegister<PharmacyUserDto>(CustomMessage.PharmacyNotFoundForSignup);

            var existingUsers = await _pharmacyUserRepository.GetAllAsync(u =>
                u.UserEmail != null &&
                u.UserEmail.ToLower() == pharmacyUserDto.UserEmail.Trim().ToLower());
            if (existingUsers.Any())
                return ConflictRegister<PharmacyUserDto>(CustomMessage.EmployeeEmailAlreadyExists);

            var entity = _mapper.Map<PharmacyUser>(pharmacyUserDto);
            entity.Id = Guid.NewGuid();
            entity.PharmacyId = pharmacy.Id;
            entity.UserEmail = pharmacyUserDto.UserEmail.Trim();
            entity.Password = PharmacyUserPasswordHelper.HashPassword(
                entity,
                pharmacyUserDto.Password);
            entity.IsActive = true;
            entity.CreatedDate = DateTime.UtcNow;

            await _pharmacyUserRepository.UpsertAsync(entity, "Id");

            var responseDto = _mapper.Map<PharmacyUserDto>(entity);
            ClearPasswordFields(responseDto);

            return new SuccessResult<PharmacyUserDto>
            {
                Data = responseDto,
                Status = (int)HttpStatusCode.OK,
                Message = CustomMessage.Registersuccess
            };
        }

        private static void ClearPasswordFields(PharmacyUserDto? dto)
        {
            if (dto == null)
                return;

            dto.Password = null;
            dto.ConfirmPassword = null;
        }

        private static SuccessResult<T> BadRegister<T>(string message) where T : class =>
            new() { Data = default, Status = (int)HttpStatusCode.BadRequest, Message = message };

        private static SuccessResult<T> ConflictRegister<T>(string message) where T : class =>
            new() { Data = default, Status = (int)HttpStatusCode.Conflict, Message = message };
    }
}
