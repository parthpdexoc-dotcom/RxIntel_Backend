using Microsoft.Extensions.Configuration;
using Rxintel.Generic.Common.Models;

namespace Rxintel.Generic.Common.Helpers;

public static class EmailSettingsResolver
{
    public static EmailSettings Resolve(IConfiguration configuration)
    {
        var settings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>()
            ?? new EmailSettings();

        if (string.IsNullOrWhiteSpace(settings.FromName))
            settings.FromName = "RxIntel";

        if (!string.IsNullOrWhiteSpace(settings.SmtpUser))
            settings.SmtpUser = settings.SmtpUser.Trim();

        if (!string.IsNullOrWhiteSpace(settings.SmtpPassword))
            settings.SmtpPassword = settings.SmtpPassword.Trim();

        if (string.IsNullOrWhiteSpace(settings.FromEmail) && !string.IsNullOrWhiteSpace(settings.SmtpUser))
            settings.FromEmail = settings.SmtpUser;

        if (!string.IsNullOrWhiteSpace(settings.FromEmail))
            settings.FromEmail = settings.FromEmail.Trim();

        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            var hostSource = !string.IsNullOrWhiteSpace(settings.FromEmail)
                ? settings.FromEmail
                : settings.SmtpUser;
            settings.SmtpHost = !string.IsNullOrWhiteSpace(hostSource)
                ? ResolveSmtpHost(hostSource)
                : "smtp.gmail.com";
        }

        if (settings.SmtpPort <= 0)
            settings.SmtpPort = 587;

        settings.SmtpHost = settings.SmtpHost.Trim();
        return settings;
    }

    private static string ResolveSmtpHost(string email)
    {
        var domain = email
            .Split('@', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault()
            ?.ToLowerInvariant();

        return domain switch
        {
            "gmail.com" or "googlemail.com" => "smtp.gmail.com",
            "outlook.com" or "hotmail.com" or "live.com" => "smtp.office365.com",
            "yahoo.com" or "ymail.com" => "smtp.mail.yahoo.com",
            _ => string.IsNullOrWhiteSpace(domain) ? "smtp.gmail.com" : $"smtp.{domain}"
        };
    }
}
