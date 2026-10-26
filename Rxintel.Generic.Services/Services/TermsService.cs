using AutoMapper;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Models.Constants;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;
using System.Net;

namespace Rxintel.Generic.Services.Services
{
    public class TermsService : ITermsService
    {
        private readonly ITermsAndConditionsRepository _termsRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenParser _tokenParser;
        private readonly IMapper _mapper;

        public TermsService(
            ITermsAndConditionsRepository termsRepository,
            IAdminRepository adminRepository,
            IUnitOfWork unitOfWork,
            ITokenParser tokenParser,
            IMapper mapper)
        {
            _termsRepository = termsRepository;
            _adminRepository = adminRepository;
            _unitOfWork = unitOfWork;
            _tokenParser = tokenParser;
            _mapper = mapper;
        }

        public async Task<TermsAndConditionsDto?> GetCurrentAsync()
        {
            var entity = await _termsRepository.GetCurrentAsync();
            if (entity == null)
                return null;

            return await MapWithAdminName(entity);
        }

        public async Task<SuccessResult<TermsAndConditionsDto>> UpsertCurrentAsync(TermsAndConditionsUpdateDto dto)
        {
            if (!_tokenParser.IsSuperAdmin())
            {
                return new SuccessResult<TermsAndConditionsDto>
                {
                    Data = null,
                    Status = (int)HttpStatusCode.Forbidden,
                    Message = CustomMessage.TermsUpdateForbidden
                };
            }

            if (dto == null || string.IsNullOrWhiteSpace(dto.Content))
            {
                return new SuccessResult<TermsAndConditionsDto>
                {
                    Data = null,
                    Status = (int)HttpStatusCode.BadRequest,
                    Message = ModelValidationErrorCodes.REQUIRED_TERMS_CONTENT
                };
            }

            var adminId = _tokenParser.GetUserId();
            var current = await _termsRepository.GetCurrentAsync();
            TermsAndConditions entity;

            if (current == null)
            {
                entity = new TermsAndConditions
                {
                    Id = Guid.NewGuid(),
                    Content = dto.Content.Trim(),
                    UpdatedByAdminId = adminId,
                    CreatedDate = DateTime.UtcNow
                };
                await _termsRepository.AddAsync(entity);
            }
            else
            {
                await _termsRepository.UpdateAsync(
                    t => t.Id == current.Id,
                    t =>
                    {
                        t.Content = dto.Content.Trim();
                        t.UpdatedByAdminId = adminId;
                        t.ModifiedDate = DateTime.UtcNow;
                    });
                entity = current;
                entity.Content = dto.Content.Trim();
                entity.UpdatedByAdminId = adminId;
                entity.ModifiedDate = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();

            var mapped = await MapWithAdminName(entity);
            return new SuccessResult<TermsAndConditionsDto>
            {
                Data = mapped,
                Status = (int)HttpStatusCode.OK,
                Message = CustomMessage.GetUpdateMessage("Terms and Conditions")
            };
        }

        private async Task<TermsAndConditionsDto> MapWithAdminName(TermsAndConditions entity)
        {
            var dto = _mapper.Map<TermsAndConditionsDto>(entity);
            if (entity.UpdatedByAdminId.HasValue && entity.UpdatedByAdminId.Value != Guid.Empty)
            {
                var admin = await _adminRepository.GetByIdAsync(entity.UpdatedByAdminId.Value, "Id");
                dto.UpdatedByAdminName = admin?.Name;
            }

            return dto;
        }
    }
}
