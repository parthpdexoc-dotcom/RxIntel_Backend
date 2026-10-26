using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Common.Helpers;

public static class WholesalerEmailHelper
{
    public static string BuildAddedByPharmacySubject(string pharmacyName, string wholesalerName) =>
        $"RxIntel - Wholesaler Added for Verification: {wholesalerName} ({pharmacyName})";

    public static string BuildAddedByPharmacyHtmlBody(
        string adminName,
        string pharmacyName,
        string wholesalerName,
        string? wholesalerAddress,
        string? wholesalerPhone,
        string? wholesalerEmail,
        bool vawd,
        DateTime? wholesalerDate,
        string? addedByName,
        string? addedByEmail)
    {
        var sb = EmailHtmlBuilder.StartDocument();
        EmailHtmlBuilder.AppendGreeting(sb, adminName);
        EmailHtmlBuilder.AppendParagraph(
            sb,
            $"Pharmacy \"{pharmacyName}\" has added a new wholesaler. Please verify the wholesaler details below.");

        EmailHtmlBuilder.AppendSection(sb, "Pharmacy", table =>
        {
            EmailHtmlBuilder.AppendRow(table, "Pharmacy", pharmacyName);
            EmailHtmlBuilder.AppendRow(table, "Added By", string.IsNullOrWhiteSpace(addedByName) ? "—" : addedByName);
            EmailHtmlBuilder.AppendRow(table, "Added By Email", string.IsNullOrWhiteSpace(addedByEmail) ? "—" : addedByEmail);
        }, $"{EmailHtmlBuilder.TableStyle}margin-bottom:24px;");

        EmailHtmlBuilder.AppendSection(sb, "Wholesaler Details", table =>
        {
            EmailHtmlBuilder.AppendRow(table, "Wholesaler Name", wholesalerName);
            EmailHtmlBuilder.AppendRow(table, "Address", string.IsNullOrWhiteSpace(wholesalerAddress) ? "—" : wholesalerAddress);
            EmailHtmlBuilder.AppendRow(table, "Phone", string.IsNullOrWhiteSpace(wholesalerPhone) ? "—" : wholesalerPhone);
            EmailHtmlBuilder.AppendRow(table, "Email", string.IsNullOrWhiteSpace(wholesalerEmail) ? "—" : wholesalerEmail);
            EmailHtmlBuilder.AppendRow(table, "VAWD", vawd ? "Yes" : "No");
            EmailHtmlBuilder.AppendRow(
                table,
                "Vawd Date",
                wholesalerDate.HasValue ? wholesalerDate.Value.ToString("yyyy-MM-dd") : "—");
        });

        EmailHtmlBuilder.AppendParagraph(
            sb,
            "Please review and verify this wholesaler in RxIntel.");

        return EmailHtmlBuilder.Finish(sb);
    }

    public static string BuildExpiryReminderSubject(
        string wholesalerName,
        WholesalerExpiryReminderKind kind) =>
        kind switch
        {
            WholesalerExpiryReminderKind.OneMonthBefore =>
                $"RxIntel - Wholesaler VAWD Expires in 1 Month: {wholesalerName}",
            WholesalerExpiryReminderKind.OneWeekBefore =>
                $"RxIntel - Wholesaler VAWD Expires in 1 Week: {wholesalerName}",
            WholesalerExpiryReminderKind.OneDayBefore =>
                $"RxIntel - Wholesaler VAWD Expires Tomorrow: {wholesalerName}",
            WholesalerExpiryReminderKind.OneMonthAfter =>
                $"RxIntel - Reminder: Wholesaler VAWD Date Has Expired: {wholesalerName}",
            _ => $"RxIntel - Wholesaler VAWD Expiry Reminder: {wholesalerName}"
        };

    public static string BuildExpiryReminderHtmlBody(
        string adminName,
        string wholesalerName,
        string? wholesalerAddress,
        string? wholesalerPhone,
        bool vawd,
        DateTime? vawdDate,
        DateTime expiryDate,
        WholesalerExpiryReminderKind kind)
    {
        var sb = EmailHtmlBuilder.StartDocument();
        EmailHtmlBuilder.AppendGreeting(sb, adminName);
        EmailHtmlBuilder.AppendParagraph(sb, BuildExpiryReminderIntro(wholesalerName, expiryDate, kind));

        EmailHtmlBuilder.AppendSection(sb, "Wholesaler Details", table =>
        {
            EmailHtmlBuilder.AppendRow(table, "Wholesaler Name", wholesalerName);
            EmailHtmlBuilder.AppendRow(table, "Address", string.IsNullOrWhiteSpace(wholesalerAddress) ? "—" : wholesalerAddress);
            EmailHtmlBuilder.AppendRow(table, "Phone", string.IsNullOrWhiteSpace(wholesalerPhone) ? "—" : wholesalerPhone);
            EmailHtmlBuilder.AppendRow(table, "VAWD", vawd ? "Yes" : "No");
            EmailHtmlBuilder.AppendRow(table, "Vawd Date", FormatDate(vawdDate));
            EmailHtmlBuilder.AppendRow(table, "Expiry Date", FormatDate(expiryDate));
            EmailHtmlBuilder.AppendRow(table, "Reminder", ToReminderLabel(kind));
        });

        EmailHtmlBuilder.AppendParagraph(
            sb,
            kind == WholesalerExpiryReminderKind.OneMonthAfter
                ? "Please review this wholesaler in RxIntel and update the Vawd Date."
                : "Please review this wholesaler in RxIntel and renew the Vawd Date before it expires.");

        return EmailHtmlBuilder.Finish(sb);
    }

    private static string BuildExpiryReminderIntro(
        string wholesalerName,
        DateTime expiryDate,
        WholesalerExpiryReminderKind kind) =>
        kind switch
        {
            WholesalerExpiryReminderKind.OneMonthBefore =>
                $"This is a reminder that the VAWD date for wholesaler \"{wholesalerName}\" will expire in 1 month on {FormatDate(expiryDate)}.",
            WholesalerExpiryReminderKind.OneWeekBefore =>
                $"This is a reminder that the VAWD date for wholesaler \"{wholesalerName}\" will expire in 1 week on {FormatDate(expiryDate)}.",
            WholesalerExpiryReminderKind.OneDayBefore =>
                $"This is a reminder that the VAWD date for wholesaler \"{wholesalerName}\" will expire tomorrow on {FormatDate(expiryDate)}.",
            WholesalerExpiryReminderKind.OneMonthAfter =>
                $"This is a reminder that the VAWD date for wholesaler \"{wholesalerName}\" expired 1 month ago on {FormatDate(expiryDate)}. Please update the Vawd Date.",
            _ =>
                $"This is a VAWD expiry reminder for wholesaler \"{wholesalerName}\"."
        };

    private static string ToReminderLabel(WholesalerExpiryReminderKind kind) =>
        kind switch
        {
            WholesalerExpiryReminderKind.OneMonthBefore => "1 month before expiry",
            WholesalerExpiryReminderKind.OneWeekBefore => "1 week before expiry",
            WholesalerExpiryReminderKind.OneDayBefore => "1 day before expiry",
            WholesalerExpiryReminderKind.OneMonthAfter => "1 month after expiry",
            _ => "Expiry reminder"
        };

    private static string FormatDate(DateTime? value) =>
        value.HasValue ? value.Value.ToString("dd/MM/yyyy") : "—";
}
