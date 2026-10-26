using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Rxintel.Generic.Common.Implementations.Helpers;
using Rxintel.Generic.Common.Models;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services;

public class EmailService : IEmailService
{
    private readonly Configurations _configurations;
    private readonly ILogger<EmailService> _logger;
    private EmailSettings? _cachedSettings;

    public EmailService(
        Configurations configurations,
        ILogger<EmailService> logger)
    {
        _configurations = configurations;
        _logger = logger;
    }

    public Task<EmailSendResult> SendEmail(string toEmail, string? toName, string subject, string htmlBody)
    {
        var settings = GetSettings();

        if (!settings.IsSmtpConfigured)
        {
            const string error =
                "SMTP is not configured. Set EmailSettings:SmtpUser, SmtpPassword, and FromEmail in appsettings.json (or environment-specific configuration).";
            _logger.LogWarning(error);
            return Task.FromResult(new EmailSendResult { ErrorMessage = error });
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
        message.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        return SendViaSmtpAsync(settings, message, toEmail);
    }

    private async Task<EmailSendResult> SendViaSmtpAsync(EmailSettings settings, MimeMessage message, string toEmail)
    {
        try
        {
            using var client = new SmtpClient { Timeout = 30000 };
            var ssl = settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

            await client.ConnectAsync(settings.SmtpHost.Trim(), settings.SmtpPort, ssl);
            await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent via SMTP from {FromEmail} to {Recipient}", settings.FromEmail, toEmail);
            return new EmailSendResult { SmtpSent = true };
        }
        catch (Exception ex)
        {
            var errorMessage = GetFriendlySmtpError(ex);
            _logger.LogError(ex, "SMTP failed for {Recipient}. {ErrorMessage}", toEmail, errorMessage);
            return new EmailSendResult { ErrorMessage = errorMessage };
        }
    }

    private EmailSettings GetSettings()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        return _cachedSettings = _configurations.GetEmailSettings();
    }

    private static string GetFriendlySmtpError(Exception ex)
    {
        var message = ex.InnerException == null ? ex.Message : $"{ex.Message} {ex.InnerException.Message}";

        if (message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase))
        {
            return "Cannot connect to SMTP server. Verify EmailSettings:SmtpHost in appsettings.json and ensure the API server has internet access.";
        }

        if (message.Contains("BadCredentials", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Username and Password not accepted", StringComparison.OrdinalIgnoreCase)
            || message.Contains("535", StringComparison.OrdinalIgnoreCase))
        {
            return "SMTP authentication failed. Verify EmailSettings:SmtpUser and SmtpPassword in appsettings.json (use a Google App Password for Gmail accounts).";
        }

        return message;
    }
}
