namespace Rxintel.Generic.Common.Models;

public class EmailSendResult
{
    public bool SmtpSent { get; set; }

    public string? ErrorMessage { get; set; }
}
