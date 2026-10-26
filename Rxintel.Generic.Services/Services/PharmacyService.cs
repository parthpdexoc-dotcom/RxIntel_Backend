using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Exceptions;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Implementations.Helpers;
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
    public class PharmacyService : GenericService<PharmacyDto, Pharmacy>, IPharmacyService
    {
        private readonly IPharmacyRepository _pharmacyRepository;
        private readonly IPharmacyUserRepository _pharmacyUserRepository;
        private readonly IPharmacyWholesalerRepository _pharmacyWholesalerRepository;
        private readonly IPharmacyRegistrationService _pharmacyRegistrationService;
        private readonly IMapper _mapper;
        private readonly Configurations _configurations;
        private readonly IStateRepository _stateRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<PharmacyService> _logger;
        private readonly IWebHostEnvironment _environment;

        public PharmacyService(
            IPharmacyRepository pharmacyRepository,
            IPharmacyUserRepository pharmacyUserRepository,
            IPharmacyWholesalerRepository pharmacyWholesalerRepository,
            IPharmacyRegistrationService pharmacyRegistrationService,
            IMapper mapper,
            Configurations configurations,
            IStateRepository stateRepository,
            IEmailService emailService,
            IWebHostEnvironment environment,
            ILogger<PharmacyService> logger)
            : base(pharmacyRepository, mapper, "Id")
        {
            _pharmacyRepository = pharmacyRepository;
            _pharmacyUserRepository = pharmacyUserRepository;
            _pharmacyWholesalerRepository = pharmacyWholesalerRepository;
            _pharmacyRegistrationService = pharmacyRegistrationService;
            _mapper = mapper;
            _configurations = configurations;
            _stateRepository = stateRepository;
            _emailService = emailService;
            _logger = logger;
            _environment = environment;
        }

        public async Task<PagedList<PharmacyDto>> GetAll(PageParam pageParam)
        {
            Expression<Func<Pharmacy, bool>>? filter = pageParam?.IsActive.HasValue == true
                ? p => p.IsActive == pageParam.IsActive.Value
                : null;

            var includes = new Expression<Func<Pharmacy, object>>[] { p => p.State!, p => p.PharmacyBillingSoftware! };
            var hasSearch = !string.IsNullOrWhiteSpace(pageParam?.SearchText);

            if (!hasSearch && pageParam != null && pageParam.IsPaginationRequested)
            {
                var totalRecords = await _pharmacyRepository.GetFilteredCountAsync(filter, includes);
                var pageEntities = (await _pharmacyRepository.GetAllAsync(filter, includes, pageParam)).ToList();
                var pageData = _mapper.Map<List<PharmacyDto>>(pageEntities);
                foreach (var pharmacy in pageData)
                    PopulateDocumentMetadata(pharmacy);
                return new PagedList<PharmacyDto>(pageData, pageParam, totalRecords);
            }

            var entities = (await _pharmacyRepository.GetAllAsync(filter, includes)).ToList();

            entities = entities.FilterBySearch(
                pageParam?.SearchText,
                p => p.BusinessTradeName,
                p => p.DEA,
                p => p.City,
                p => p.PhoneNumber,
                p => p.Email,
                p => p.State?.StateName,
                p => p.PharmacyBillingSoftware?.SoftwareName,
                p => p.PharmacyBillingSoftwareName);

            var data = _mapper.Map<List<PharmacyDto>>(entities);
            foreach (var pharmacy in data)
                PopulateDocumentMetadata(pharmacy);

            return ToPagedList(data, pageParam);
        }

        public override async Task<PharmacyDto> GetByIdAsync(Guid id)
        {
            var entity = await _pharmacyRepository.GetByIdAsync(id, "Id", p => p.State!);
            if (entity == null)
                return null!;

            var dto = _mapper.Map<PharmacyDto>(entity);
            PopulateDocumentMetadata(dto);
            return dto;
        }

        public async Task<string?> GetDeaDocumentPhysicalPathAsync(Guid pharmacyId) =>
            await GetDocumentPhysicalPathAsync(pharmacyId, PharmacyDocumentStorageHelper.DeaDocumentName);

        public async Task<string?> GetStateBoardLicenseDocumentPhysicalPathAsync(Guid pharmacyId) =>
            await GetDocumentPhysicalPathAsync(pharmacyId, PharmacyDocumentStorageHelper.LicenseDocumentName);

        public override async Task<SuccessResult<PharmacyDto>> UpsertAsync(PharmacyDto pharmacyDto)
            => await UpsertPharmacy(pharmacyDto);

        public async Task<SuccessResult<PharmacyDto>> UpsertPharmacy(
            PharmacyDto pharmacyDto,
            IFormFile? deaDocument = null,
            IFormFile? stateBoardLicenseDocument = null)
        {
            if (pharmacyDto == null)
                return SuccessResult<PharmacyDto>.BadRequest(CustomMessage.GetInvalidMessage("Pharmacy"));

            var documentError = ValidateOptionalDocumentUploads(deaDocument, stateBoardLicenseDocument);
            if (documentError != null)
                return documentError;

            var isUpdate = pharmacyDto.Id != Guid.Empty;
            Pharmacy? existingPharmacy = null;
            if (isUpdate)
                existingPharmacy = await _pharmacyRepository.GetByIdAsync(pharmacyDto.Id, "Id");

            var result = await base.UpsertAsync(pharmacyDto);
            var pharmacyId = result.Data?.Id ?? Guid.Empty;
            if (pharmacyId != Guid.Empty)
            {
                var pharmacyEmail = (result.Data?.Email ?? string.Empty).Trim().ToLower();
                var contactEmail = (result.Data?.UserEmail ?? string.Empty).Trim().ToLower();
                await _pharmacyUserRepository.UpdateAsync(
                    u =>
                        u.PharmacyId == pharmacyId ||
                        (u.UserEmail != null && u.UserEmail.ToLower() == contactEmail) ||
                        (u.UserEmail != null && u.UserEmail.ToLower() == pharmacyEmail),
                    u => u.IsActive = result.Data!.IsActive);

                if (PharmacyDocumentStorageHelper.HasFileContent(deaDocument))
                {
                    await PharmacyDocumentStorageHelper.SaveDocument(
                        _environment.ContentRootPath,
                        pharmacyId,
                        PharmacyDocumentStorageHelper.DeaDocumentName,
                        deaDocument!);
                }

                if (PharmacyDocumentStorageHelper.HasFileContent(stateBoardLicenseDocument))
                {
                    await PharmacyDocumentStorageHelper.SaveDocument(
                        _environment.ContentRootPath,
                        pharmacyId,
                        PharmacyDocumentStorageHelper.LicenseDocumentName,
                        stateBoardLicenseDocument!);
                }
            }

            if (isUpdate && result.Data != null)
                await NotifyPharmacyOfUpdate(result.Data, existingPharmacy);

            if (result.Data != null)
            {
                result.Data.StateName = await ResolveStateName(result.Data.StateId);
                PopulateDocumentMetadata(result.Data);
            }

            return result;
        }

        public override async Task<SuccessResult<PharmacyDto>> DeleteAsync(Guid id)
        {
            if ((await _pharmacyWholesalerRepository.GetAllAsync(x => x.PharmacyId == id)).Any())
                return SuccessResult<PharmacyDto>.BadRequest(CustomMessage.PharmacyDeleteBlockedByWholesalerLink);

            return await base.DeleteAsync(id);
        }

        public async Task<TokenDto> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new WebApiException(HttpStatusCode.BadRequest, CustomMessage.GetInvalidMessage("User"));

            var normalizedEmail = email.Trim().ToLower();
            var users = await _pharmacyUserRepository.GetAllAsync(u =>
                u.UserEmail != null &&
                u.UserEmail.ToLower() == normalizedEmail);
            var user = users.FirstOrDefault();

            if (user == null || !PharmacyUserPasswordHelper.VerifyPassword(user, password, out var rehashNeeded))
                throw new WebApiException(HttpStatusCode.Unauthorized, CustomMessage.InvalidEmployeeUser);

            if (rehashNeeded)
            {
                user.Password = PharmacyUserPasswordHelper.HashPassword(user, password);
                await _pharmacyUserRepository.UpsertAsync(user, "Id");
            }

            if (!user.IsActive)
                throw new WebApiException(HttpStatusCode.Unauthorized, CustomMessage.EmployeeAccountInactive);

            var token = TokenHelper.GenerateEmployeeToken(_configurations, user);
            var expiresAt = DateTime.UtcNow.AddMinutes(TokenHelper.GetTokenExpirationInMinutes(_configurations));

            return new TokenDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                TokenType = "Bearer"
            };
        }

        public Task<SuccessResult<PharmacyDto>> RegisterPharmacy(
            PharmacyDto pharmacyDto,
            IFormFile? deaDocument = null,
            IFormFile? stateBoardLicenseDocument = null) =>
            _pharmacyRegistrationService.RegisterPharmacy(pharmacyDto, deaDocument, stateBoardLicenseDocument);

        private async Task NotifyPharmacyOfUpdate(PharmacyDto pharmacy, Pharmacy? previousPharmacy)
        {
            var pharmacyEmail = (pharmacy.Email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(pharmacyEmail))
            {
                _logger.LogWarning(
                    "Pharmacy update notification skipped because pharmacy {PharmacyId} has no email.",
                    pharmacy.Id);
                return;
            }

            var pharmacyName = string.IsNullOrWhiteSpace(pharmacy.BusinessTradeName)
                ? "Pharmacy"
                : pharmacy.BusinessTradeName.Trim();
            var statusChanged = previousPharmacy != null && previousPharmacy.IsActive != pharmacy.IsActive;
            var subject = PharmacyEmailHelper.BuildUpdateSubject(pharmacyName, pharmacy.IsActive, statusChanged);
            var htmlBody = PharmacyEmailHelper.BuildUpdateHtmlBody(pharmacyName, pharmacy.IsActive, statusChanged);

            await SendNotificationEmail(pharmacyEmail, pharmacyName, subject, htmlBody, "Pharmacy update notification");
        }

        private async Task SendNotificationEmail(
            string toEmail,
            string? toName,
            string subject,
            string htmlBody,
            string notificationType)
        {
            var sendResult = await _emailService.SendEmail(toEmail, toName, subject, htmlBody);
            if (sendResult.SmtpSent)
                return;

            var errorMessage = string.IsNullOrWhiteSpace(sendResult.ErrorMessage)
                ? CustomMessage.EmailNotificationSmtpFailed
                : sendResult.ErrorMessage;

             _logger.LogError(
                "{NotificationType} was not delivered via SMTP to {Recipient}. Error={Error}",
                notificationType, toEmail, errorMessage);
        }

        private async Task<string?> ResolveStateName(Guid? stateId)
        {
            if (!stateId.HasValue || stateId.Value == Guid.Empty)
                return null;

            var states = await _stateRepository.GetAllAsync(s => s.StateId == stateId.Value);
            return states.FirstOrDefault()?.StateName;
        }

        private SuccessResult<PharmacyDto>? ValidateOptionalDocumentUploads(
            IFormFile? deaDocument,
            IFormFile? stateBoardLicenseDocument)
        {
            var maxSize = _configurations.PharmacyDocumentMaxFileSizeBytes;

            if (PharmacyDocumentStorageHelper.HasFileContent(deaDocument))
            {
                if (!PharmacyDocumentStorageHelper.IsAllowedDocument(deaDocument))
                    return BadRegister<PharmacyDto>(ModelValidationErrorCodes.INVALID_PHARMACY_DOCUMENT_FORMAT);
                if (!PharmacyDocumentStorageHelper.IsWithinSizeLimit(deaDocument, maxSize))
                    return BadRegister<PharmacyDto>(ModelValidationErrorCodes.INVALID_PHARMACY_DOCUMENT_SIZE);
            }

            if (PharmacyDocumentStorageHelper.HasFileContent(stateBoardLicenseDocument))
            {
                if (!PharmacyDocumentStorageHelper.IsAllowedDocument(stateBoardLicenseDocument))
                    return BadRegister<PharmacyDto>(ModelValidationErrorCodes.INVALID_PHARMACY_DOCUMENT_FORMAT);
                if (!PharmacyDocumentStorageHelper.IsWithinSizeLimit(stateBoardLicenseDocument, maxSize))
                    return BadRegister<PharmacyDto>(ModelValidationErrorCodes.INVALID_PHARMACY_DOCUMENT_SIZE);
            }

            return null;
        }

        private void PopulateDocumentMetadata(PharmacyDto pharmacyDto)
        {
            if (pharmacyDto.Id == Guid.Empty)
                return;

            var deaFileName = PharmacyDocumentStorageHelper.GetDocumentFileName(
                _environment.ContentRootPath,
                pharmacyDto.Id,
                PharmacyDocumentStorageHelper.DeaDocumentName);

            pharmacyDto.HasDeaFile = deaFileName != null;
            pharmacyDto.DeaFileName = deaFileName;

            var stateBoardFileName = PharmacyDocumentStorageHelper.GetDocumentFileName(
                _environment.ContentRootPath,
                pharmacyDto.Id,
                PharmacyDocumentStorageHelper.LicenseDocumentName);

            pharmacyDto.HasStateBoardFile = stateBoardFileName != null;
            pharmacyDto.StateBoardFileName = stateBoardFileName;
        }

        private async Task<string?> GetDocumentPhysicalPathAsync(Guid pharmacyId, string documentName)
        {
            if (pharmacyId == Guid.Empty)
                return null;

            var pharmacy = await _pharmacyRepository.GetByIdAsync(pharmacyId, "Id");
            if (pharmacy == null)
                return null;

            return PharmacyDocumentStorageHelper.FindDocumentPath(
                _environment.ContentRootPath,
                pharmacyId,
                documentName);
        }

        private static SuccessResult<T> BadRegister<T>(string message) where T : class =>
            new() { Data = default, Status = (int)HttpStatusCode.BadRequest, Message = message };
    }
}
