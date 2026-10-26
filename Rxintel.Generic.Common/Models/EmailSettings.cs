namespace Rxintel.Generic.Common.Models;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string SmtpHost { get; set; } = "smtp.gmail.com";

    public int SmtpPort { get; set; } = 587;

    public string SmtpUser { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "RxIntel";

    public bool IsSmtpConfigured =>
        !string.IsNullOrWhiteSpace(SmtpHost) &&
        !string.IsNullOrWhiteSpace(FromEmail) &&
        !string.IsNullOrWhiteSpace(SmtpUser) &&
        !string.IsNullOrWhiteSpace(SmtpPassword);
}
