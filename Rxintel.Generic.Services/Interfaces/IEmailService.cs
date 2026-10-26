using Rxintel.Generic.Common.Models;

namespace Rxintel.Generic.Services.Interfaces;

public interface IEmailService
{
    Task<EmailSendResult> SendEmail(string toEmail, string? toName, string subject, string htmlBody);
}
