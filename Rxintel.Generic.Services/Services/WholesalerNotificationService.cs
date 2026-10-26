using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services;

public class WholesalerNotificationService : IWholesalerNotificationService
{
    private readonly IEmailService _emailService;
    private readonly IAdminRepository _adminRepository;
    private readonly IPharmacyRepository _pharmacyRepository;
    private readonly IWholesalerRepository _wholesalerRepository;
    private readonly ILogger<WholesalerNotificationService> _logger;

    public WholesalerNotificationService(
        IEmailService emailService,
        IAdminRepository adminRepository,
        IPharmacyRepository pharmacyRepository,
        IWholesalerRepository wholesalerRepository,
        ILogger<WholesalerNotificationService> logger)
    {
        _emailService = emailService;
        _adminRepository = adminRepository;
        _pharmacyRepository = pharmacyRepository;
        _wholesalerRepository = wholesalerRepository;
        _logger = logger;
    }

    public async Task NotifyAdminsWholesalerAddedByPharmacyAsync(
        Guid pharmacyId,
        WholesalerDto wholesaler,
        string? addedByName,
        string? addedByEmail)
    {
        if (wholesaler == null)
            return;

        var adminRecipients = await GetAdminRecipientsAsync();
        if (adminRecipients.Count == 0)
        {
            _logger.LogWarning("Wholesaler-added notification skipped because no admin recipients were found.");
            return;
        }

        var pharmacyName = await ResolvePharmacyNameAsync(pharmacyId);
        var wholesalerName = string.IsNullOrWhiteSpace(wholesaler.WholesalerName)
            ? "Unknown wholesaler"
            : wholesaler.WholesalerName.Trim();
        var subject = WholesalerEmailHelper.BuildAddedByPharmacySubject(pharmacyName, wholesalerName);

        foreach (var (adminEmail, adminName) in adminRecipients)
        {
            var htmlBody = WholesalerEmailHelper.BuildAddedByPharmacyHtmlBody(
                adminName,
                pharmacyName,
                wholesalerName,
                wholesaler.Address,
                wholesaler.Phone,
                wholesaler.Email,
                wholesaler.Vawd,
                wholesaler.VawdDate,
                addedByName,
                addedByEmail);

            await SendNotificationEmailAsync(
                adminEmail,
                adminName,
                subject,
                htmlBody,
                "Wholesaler added by pharmacy notification");
        }
    }

    public async Task SendDueExpiryRemindersAsync(CancellationToken cancellationToken = default)
    {
        var wholesalers = (await _wholesalerRepository.GetAllAsync(w => w.ExpiryDate != null)).ToList();
        if (wholesalers.Count == 0)
            return;

        var today = DateTime.Today;
        var dueWholesalers = new List<(Wholesaler Wholesaler, WholesalerExpiryReminderKind Kind, DateTime ExpiryDate)>();

        foreach (var wholesaler in wholesalers)
        {
            var expiryDate = wholesaler.ExpiryDate!.Value.Date;
            foreach (var kind in WholesalerVawdExpiryHelper.GetDueReminderKinds(today, expiryDate))
                dueWholesalers.Add((wholesaler, kind, expiryDate));
        }

        if (dueWholesalers.Count == 0)
            return;

        var adminRecipients = await GetAdminRecipientsAsync();
        if (adminRecipients.Count == 0)
        {
            _logger.LogWarning("Wholesaler expiry reminders skipped because no admin recipients were found.");
            return;
        }

        foreach (var (wholesaler, kind, expiryDate) in dueWholesalers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var wholesalerName = string.IsNullOrWhiteSpace(wholesaler.WholesalerName)
                ? "Unknown wholesaler"
                : wholesaler.WholesalerName.Trim();
            var subject = WholesalerEmailHelper.BuildExpiryReminderSubject(wholesalerName, kind);

            foreach (var (adminEmail, adminName) in adminRecipients)
            {
                var htmlBody = WholesalerEmailHelper.BuildExpiryReminderHtmlBody(
                    adminName,
                    wholesalerName,
                    wholesaler.Address,
                    wholesaler.Phone,
                    wholesaler.Vawd,
                    wholesaler.VawdDate,
                    expiryDate,
                    kind);

                await SendNotificationEmailAsync(
                    adminEmail,
                    adminName,
                    subject,
                    htmlBody,
                    $"Wholesaler VAWD {kind} reminder");
            }
        }
    }

    private async Task<string> ResolvePharmacyNameAsync(Guid pharmacyId)
    {
        if (pharmacyId == Guid.Empty)
            return "Unknown pharmacy";

        var pharmacy = (await _pharmacyRepository.GetAllAsync(p => p.Id == pharmacyId)).FirstOrDefault();
        if (pharmacy == null || string.IsNullOrWhiteSpace(pharmacy.BusinessTradeName))
            return "Unknown pharmacy";

        return pharmacy.BusinessTradeName.Trim();
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
            ? "SMTP send failed"
            : sendResult.ErrorMessage;

        _logger.LogError(
            "{NotificationType} was not delivered via SMTP to {Recipient}. Error={Error}",
            notificationType, toEmail, errorMessage);
    }
}
