using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Implementations.Helpers;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Models.Constants;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Helpers;
using Rxintel.Generic.Services.Interfaces;
using System.Net;

namespace Rxintel.Generic.Services.Services
{
    /// <summary>
    /// Orchestrates pharmacy + pharmacy-admin user registration (extracted from PharmacyService).
    /// </summary>
    public class PharmacyRegistrationService : IPharmacyRegistrationService
    {
        private readonly IPharmacyRepository _pharmacyRepository;
        private readonly IPharmacyUserRepository _pharmacyUserRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAdminRepository _adminRepository;
        private readonly IStateRepository _stateRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<PharmacyRegistrationService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly Configurations _configurations;
        private readonly ITermsAndConditionsRepository _termsAndConditionsRepository;

        public PharmacyRegistrationService(
            IPharmacyRepository pharmacyRepository,
            IPharmacyUserRepository pharmacyUserRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAdminRepository adminRepository,
            IStateRepository stateRepository,
            IEmailService emailService,
            IWebHostEnvironment environment,
            Configurations configurations,
            ITermsAndConditionsRepository termsAndConditionsRepository,
            ILogger<PharmacyRegistrationService> logger)
        {
            _pharmacyRepository = pharmacyRepository;
            _pharmacyUserRepository = pharmacyUserRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _adminRepository = adminRepository;
            _stateRepository = stateRepository;
            _emailService = emailService;
            _environment = environment;
            _configurations = configurations;
            _termsAndConditionsRepository = termsAndConditionsRepository;
            _logger = logger;
        }

        public async Task<SuccessResult<PharmacyDto>> RegisterPharmacy(
            PharmacyDto pharmacyDto,
            IFormFile? deaDocument = null,
            IFormFile? stateBoardLicenseDocument = null)
        {
            if (pharmacyDto == null)
                return BadRegister<PharmacyDto>(CustomMessage.GetInvalidMessage("Registration"));

            var pharmacyEmail = pharmacyDto.Email?.Trim();
            var contactEmail = pharmacyDto.UserEmail?.Trim();
            var hasDeaDocument = PharmacyDocumentStorageHelper.HasFileContent(deaDocument);
            var hasLicenseText = !string.IsNullOrWhiteSpace(pharmacyDto.StateBoardOfPharmacyLicenseNumber);
            var hasLicenseDocument = PharmacyDocumentStorageHelper.HasFileContent(stateBoardLicenseDocument);

            var documentError = ValidateOptionalDocumentUploads(deaDocument, stateBoardLicenseDocument);
            if (documentError != null)
                return documentError;

            if (!hasLicenseText && !hasLicenseDocument)
                return BadRegister<PharmacyDto>(ModelValidationErrorCodes.REQUIRED_PHARMACY_STATE_BOARD_LICENSE_OR_DOCUMENT);

            if (string.IsNullOrWhiteSpace(pharmacyDto.Name) ||
                string.IsNullOrWhiteSpace(pharmacyDto.Password) ||
                string.IsNullOrWhiteSpace(pharmacyDto.ConfirmPassword) ||
                string.IsNullOrWhiteSpace(pharmacyDto.PrimaryContactName) ||
                string.IsNullOrWhiteSpace(pharmacyDto.PrimaryContactPhone) ||
                string.IsNullOrWhiteSpace(contactEmail))
            {
                return BadRegister<PharmacyDto>(CustomMessage.EmployeeRegistrationFieldsRequired);
            }

            if (!string.Equals(pharmacyDto.Password, pharmacyDto.ConfirmPassword, StringComparison.Ordinal))
                return BadRegister<PharmacyDto>(CustomMessage.PasswordMismatch);

            var currentTerms = await _termsAndConditionsRepository.GetCurrentAsync();
            if (currentTerms == null)
                return BadRegister<PharmacyDto>(CustomMessage.TermsNotConfigured);

            if (!pharmacyDto.AcceptedTermsAndConditions)
                return BadRegister<PharmacyDto>(CustomMessage.TermsAcceptanceRequired);

            var userName = pharmacyDto.Name.Trim();
            var role = PharmacyUserRole.PharmacyAdmin;

            var deaNorm = pharmacyDto.DEA.Trim();
            var pharmacies = await _pharmacyRepository.GetAllAsync(p => p.DEA == deaNorm);
            if (pharmacies.Any())
                return ConflictRegister<PharmacyDto>(CustomMessage.PharmacyDeaAlreadyExists);

            var normalizedContactEmail = contactEmail!.ToLower();
            var existingUsers = await _pharmacyUserRepository.GetAllAsync(u =>
                u.UserEmail != null &&
                u.UserEmail.ToLower() == normalizedContactEmail);
            if (existingUsers.Any())
                return ConflictRegister<PharmacyDto>(CustomMessage.EmployeeEmailAlreadyExists);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            Pharmacy? pharmacyEntity = null;
            PharmacyUser? userEntity = null;
            try
            {
                pharmacyEntity = new Pharmacy
                {
                    Id = Guid.NewGuid(),
                    DEA = deaNorm,
                    DeaExpirationDate = pharmacyDto.DeaExpirationDate,
                    BusinessLegalName = pharmacyDto.BusinessLegalName.Trim(),
                    BusinessTradeName = pharmacyDto.BusinessTradeName.Trim(),
                    NPI = pharmacyDto.NPI?.Trim(),
                    PharmacyAddress = pharmacyDto.PharmacyAddress.Trim(),
                    StreetAddress = pharmacyDto.StreetAddress?.Trim(),
                    City = pharmacyDto.City.Trim(),
                    StateId = pharmacyDto.StateId,
                    PharmacySoftwareId = pharmacyDto.PharmacySoftwareId,
                    ZipCode = pharmacyDto.ZipCode.Trim(),
                    PhoneNumber = pharmacyDto.PhoneNumber.Trim(),
                    Fax = pharmacyDto.Fax?.Trim(),
                    StateBoardOfPharmacyLicenseNumber = hasLicenseText
                        ? pharmacyDto.StateBoardOfPharmacyLicenseNumber!.Trim()
                        : null,
                    BopLicenseExpirationDate = pharmacyDto.BopLicenseExpirationDate,
                    GlobalLocationNumber = pharmacyDto.GlobalLocationNumber?.Trim(),
                    PharmacyBillingSoftwareName = pharmacyDto.PharmacyBillingSoftwareName?.Trim(),
                    PharmacyBillingSoftwarePhone = pharmacyDto.PharmacyBillingSoftwarePhone?.Trim(),
                    PharmacyBillingSoftwareEmail = pharmacyDto.PharmacyBillingSoftwareEmail?.Trim(),
                    Email = pharmacyEmail,
                    IsActive = false,
                    CreatedDate = DateTime.UtcNow
                };

                userEntity = new PharmacyUser
                {
                    Id = Guid.NewGuid(),
                    PharmacyId = pharmacyEntity.Id,
                    Name = userName,
                    UserEmail = contactEmail!,
                    Role = role,
                    PrimaryContactName = pharmacyDto.PrimaryContactName.Trim(),
                    PrimaryContactTitle = pharmacyDto.PrimaryContactTitle?.Trim(),
                    PrimaryContactPhone = pharmacyDto.PrimaryContactPhone.Trim(),
                    PrimaryWholesalerName = pharmacyDto.PrimaryWholesalerName?.Trim(),
                    PrimaryWholesalerContactName = pharmacyDto.PrimaryWholesalerContactName?.Trim(),
                    PrimaryWholesalerPhone = pharmacyDto.PrimaryWholesalerPhone?.Trim(),
                    PrimaryWholesalerEmail = pharmacyDto.PrimaryWholesalerEmail?.Trim(),
                    IsActive = false,
                    CreatedDate = DateTime.UtcNow
                };
                userEntity.Password = PharmacyUserPasswordHelper.HashPassword(
                    userEntity,
                    pharmacyDto.Password);

                await _pharmacyRepository.AddAsync(pharmacyEntity);
                await _pharmacyUserRepository.AddAsync(userEntity);

                await _unitOfWork.SaveChangesAsync();

                if (hasDeaDocument)
                {
                    await PharmacyDocumentStorageHelper.SaveDocument(
                        _environment.ContentRootPath,
                        pharmacyEntity.Id,
                        PharmacyDocumentStorageHelper.DeaDocumentName,
                        deaDocument!);
                }

                if (hasLicenseDocument)
                {
                    await PharmacyDocumentStorageHelper.SaveDocument(
                        _environment.ContentRootPath,
                        pharmacyEntity.Id,
                        PharmacyDocumentStorageHelper.LicenseDocumentName,
                        stateBoardLicenseDocument!);
                }

                await _unitOfWork.CommitAsync(transaction);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(transaction);
                if (pharmacyEntity != null)
                {
                    PharmacyDocumentStorageHelper.DeletePharmacyDocumentsFolder(
                        _environment.ContentRootPath,
                        pharmacyEntity.Id);
                }
                throw;
            }

            await NotifyAdminOfRegistration(pharmacyEntity!, userName, contactEmail!, role);

            var responseDto = _mapper.Map<PharmacyDto>(pharmacyEntity);
            var pharmacyId = responseDto.Id;
            _mapper.Map(userEntity, responseDto);
            responseDto.Id = pharmacyId;
            responseDto.Password = null;
            responseDto.ConfirmPassword = null;
            responseDto.StateName = await ResolveStateName(pharmacyEntity.StateId);
            PopulateDocumentMetadata(responseDto);

            return new SuccessResult<PharmacyDto>
            {
                Data = responseDto,
                Status = (int)HttpStatusCode.OK,
                Message = CustomMessage.Registersuccess
            };
        }

        private async Task NotifyAdminOfRegistration(
            Pharmacy pharmacy,
            string userName,
            string userEmail,
            PharmacyUserRole role)
        {
            var adminRecipients = await GetAdminRecipients();
            if (adminRecipients.Count == 0)
            {
                _logger.LogWarning("No admin email in Admin table.");
                return;
            }

            var stateName = await ResolveStateName(pharmacy.StateId);
            var subject = PharmacyEmailHelper.BuildRegistrationSubject(pharmacy.BusinessTradeName);
            var roleName = EnumHelper.ToStringValue(role);

            foreach (var (adminEmail, adminName) in adminRecipients)
            {
                var htmlBody = PharmacyEmailHelper.BuildRegistrationHtmlBody(
                    adminName,
                    userName,
                    userEmail,
                    roleName,
                    pharmacy.BusinessTradeName,
                    pharmacy.DEA,
                    pharmacy.NPI,
                    pharmacy.PhoneNumber,
                    pharmacy.PharmacyAddress,
                    pharmacy.StreetAddress,
                    pharmacy.City,
                    stateName,
                    pharmacy.ZipCode);

                await SendNotificationEmail(adminEmail, adminName, subject, htmlBody, "Admin registration notification");
            }
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

        private async Task<List<(string Email, string Name)>> GetAdminRecipients()
        {
            var recipients = new List<(string Email, string Name)>();
            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var admin in await _adminRepository.GetAllAsync())
            {
                if (string.IsNullOrWhiteSpace(admin.Email))
                    continue;

                var email = admin.Email.Trim();
                if (!seenEmails.Add(email))
                    continue;

                var name = string.IsNullOrWhiteSpace(admin.Name) ? "Admin" : admin.Name.Trim();
                recipients.Add((email, name));
            }

            return recipients;
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

        private static SuccessResult<T> BadRegister<T>(string message) where T : class =>
            new() { Data = default, Status = (int)HttpStatusCode.BadRequest, Message = message };

        private static SuccessResult<T> ConflictRegister<T>(string message) where T : class =>
            new() { Data = default, Status = (int)HttpStatusCode.Conflict, Message = message };
    }
}
