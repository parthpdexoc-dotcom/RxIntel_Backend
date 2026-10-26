using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Helpers;

public abstract class FieldMappingNotificationServiceBase
{
    private readonly IEmailService _emailService;
    private readonly IAdminRepository _adminRepository;
    private readonly IPharmacyRepository _pharmacyRepository;
    private readonly IPharmacyUserRepository _pharmacyUserRepository;
    private readonly ILogger _logger;

    protected FieldMappingNotificationServiceBase(
        IEmailService emailService,
        IAdminRepository adminRepository,
        IPharmacyRepository pharmacyRepository,
        IPharmacyUserRepository pharmacyUserRepository,
        ILogger logger)
    {
        _emailService = emailService;
        _adminRepository = adminRepository;
        _pharmacyRepository = pharmacyRepository;
        _pharmacyUserRepository = pharmacyUserRepository;
        _logger = logger;
    }

    protected abstract FieldMappingEmailLabels Labels { get; }

    protected abstract string NotificationArea { get; }

    protected async Task NotifyMappingMissingAsync(
        Guid pharmacyId,
        string contextName,
        string? uploaderEmail)
    {
        var adminRecipients = await GetAdminRecipientsAsync();
        if (adminRecipients.Count == 0)
        {
            _logger.LogWarning(
                "{NotificationArea} request email skipped because no admin recipients were found.",
                NotificationArea);
            return;
        }

        var pharmacyName = await ResolvePharmacyNameAsync(pharmacyId);
        var subject = FieldMappingEmailHelper.BuildAdminRequestSubject(Labels, pharmacyName);

        foreach (var (adminEmail, adminName) in adminRecipients)
        {
            var htmlBody = FieldMappingEmailHelper.BuildAdminRequestHtmlBody(
                Labels,
                adminName,
                pharmacyName,
                contextName,
                uploaderEmail);

            await SendNotificationEmailAsync(
                adminEmail,
                adminName,
                subject,
                htmlBody,
                $"{NotificationArea} request notification");
        }

        await NotifyPharmacyAdminsMappingRequestSentAsync(
            pharmacyId,
            pharmacyName,
            contextName,
            uploaderEmail,
            adminRecipients.Select(a => a.Email));
    }

    protected async Task NotifyPharmacyUsersMappingConfiguredAsync(
        string contextName,
        IReadOnlyList<Pharmacy> pharmacies,
        string emptyPharmaciesLogMessage,
        string emptyUsersLogMessage)
    {
        if (pharmacies.Count == 0)
        {
            _logger.LogInformation("{Message}", emptyPharmaciesLogMessage);
            return;
        }

        var recipients = new List<(string Email, string Name, string PharmacyName)>();
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pharmacy in pharmacies)
        {
            var pharmacyName = GetPharmacyDisplayName(pharmacy);
            var users = await _pharmacyUserRepository.GetAllAsync(
                u => u.PharmacyId == pharmacy.Id && u.IsActive);

            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(user.UserEmail))
                    continue;

                var email = user.UserEmail.Trim();
                if (!seenEmails.Add(email))
                    continue;

                var name = string.IsNullOrWhiteSpace(user.Name) ? pharmacyName : user.Name.Trim();
                recipients.Add((email, name, pharmacyName));
            }
        }

        if (recipients.Count == 0)
        {
            _logger.LogWarning("{Message}", emptyUsersLogMessage);
            return;
        }

        foreach (var (email, name, pharmacyName) in recipients)
        {
            var subject = FieldMappingEmailHelper.BuildPharmacyUserCompletedSubject(Labels, pharmacyName);
            var htmlBody = FieldMappingEmailHelper.BuildPharmacyUserCompletedHtmlBody(
                Labels,
                name,
                pharmacyName,
                contextName);

            await SendNotificationEmailAsync(
                email,
                name,
                subject,
                htmlBody,
                $"{NotificationArea} completion notification");
        }
    }

    protected async Task<string> ResolvePharmacyNameAsync(Guid pharmacyId)
    {
        if (pharmacyId == Guid.Empty)
            return "Pharmacy";

        var pharmacy = await _pharmacyRepository.GetByIdAsync(pharmacyId, "Id");
        return pharmacy == null ? "Pharmacy" : GetPharmacyDisplayName(pharmacy);
    }

    protected IPharmacyRepository PharmacyRepository => _pharmacyRepository;

    protected static string GetPharmacyDisplayName(Pharmacy pharmacy)
    {
        if (!string.IsNullOrWhiteSpace(pharmacy.BusinessTradeName))
            return pharmacy.BusinessTradeName.Trim();

        if (!string.IsNullOrWhiteSpace(pharmacy.BusinessLegalName))
            return pharmacy.BusinessLegalName.Trim();

        return "Pharmacy";
    }

    private async Task NotifyPharmacyAdminsMappingRequestSentAsync(
        Guid pharmacyId,
        string pharmacyName,
        string contextName,
        string? uploaderEmail,
        IEnumerable<string> adminEmailsAlreadyNotified)
    {
        if (pharmacyId == Guid.Empty)
            return;

        var seenEmails = new HashSet<string>(adminEmailsAlreadyNotified, StringComparer.OrdinalIgnoreCase);
        var pharmacyAdmins = await _pharmacyUserRepository.GetAllAsync(u =>
            u.PharmacyId == pharmacyId &&
            u.IsActive &&
            u.Role == PharmacyUserRole.PharmacyAdmin);

        var recipients = new List<(string Email, string Name)>();
        foreach (var user in pharmacyAdmins)
        {
            if (string.IsNullOrWhiteSpace(user.UserEmail))
                continue;

            var email = user.UserEmail.Trim();
            if (!seenEmails.Add(email))
                continue;

            var name = string.IsNullOrWhiteSpace(user.Name) ? "Pharmacy Admin" : user.Name.Trim();
            recipients.Add((email, name));
        }

        if (recipients.Count == 0)
        {
            _logger.LogWarning(
                "{NotificationArea} pharmacy-admin confirmation skipped because no active pharmacy admins were found for pharmacy {PharmacyId}.",
                NotificationArea,
                pharmacyId);
            return;
        }

        var subject = FieldMappingEmailHelper.BuildPharmacyAdminRequestAcknowledgedSubject(Labels, pharmacyName);
        foreach (var (email, name) in recipients)
        {
            var htmlBody = FieldMappingEmailHelper.BuildPharmacyAdminRequestAcknowledgedHtmlBody(
                Labels,
                name,
                pharmacyName,
                contextName,
                uploaderEmail);

            await SendNotificationEmailAsync(
                email,
                name,
                subject,
                htmlBody,
                $"{NotificationArea} pharmacy-admin confirmation");
        }
    }

    private async Task<List<(string Email, string Name)>> GetAdminRecipientsAsync()
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

    private async Task SendNotificationEmailAsync(
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
            notificationType,
            toEmail,
            errorMessage);
    }
}
