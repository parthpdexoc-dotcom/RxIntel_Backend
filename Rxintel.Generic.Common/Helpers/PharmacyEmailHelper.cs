namespace Rxintel.Generic.Common.Helpers;

public static class PharmacyEmailHelper
{
    public static string BuildRegistrationSubject(string pharmacyName) =>
        $"RxIntel - New Pharmacy Registration: {pharmacyName}";

    public static string BuildRegistrationHtmlBody(
        string adminName,
        string userName,
        string userEmail,
        string role,
        string businessTradeName,
        string dea,
        string? npi,
        string? phoneNumber,
        string? pharmacyAddress,
        string? streetAddress,
        string? city,
        string? stateName,
        string? zipCode)
    {
        var sb = EmailHtmlBuilder.StartDocument();
        EmailHtmlBuilder.AppendGreeting(sb, adminName);
        sb.Append("<p>A new pharmacy user has registered on RxIntel. Please review the details below.</p>");
        EmailHtmlBuilder.AppendSection(sb, "User Details", table =>
        {
            EmailHtmlBuilder.AppendRow(table, "Name", userName);
            EmailHtmlBuilder.AppendRow(table, "Email", userEmail);
            EmailHtmlBuilder.AppendRow(table, "Role", role);
        }, $"{EmailHtmlBuilder.TableStyle}margin-bottom:24px;");
        EmailHtmlBuilder.AppendSection(sb, "Pharmacy Details", table =>
        {
            EmailHtmlBuilder.AppendRow(table, "BusinessTradeName", businessTradeName);
            EmailHtmlBuilder.AppendRow(table, "DEA", dea);
            EmailHtmlBuilder.AppendRow(table, "NPI", npi ?? "—");
            EmailHtmlBuilder.AppendRow(table, "PhoneNumber", phoneNumber ?? "—");
            EmailHtmlBuilder.AppendRow(table, "PharmacyAddress", pharmacyAddress ?? "—");
            EmailHtmlBuilder.AppendRow(table, "StreetAddress", streetAddress ?? "—");
            EmailHtmlBuilder.AppendRow(table, "City", city ?? "—");
            EmailHtmlBuilder.AppendRow(table, "State", stateName ?? "—");
            EmailHtmlBuilder.AppendRow(table, "ZipCode", zipCode ?? "—");
        });
        return EmailHtmlBuilder.Finish(sb);
    }

    public static string BuildUpdateSubject(string pharmacyName, bool isActive, bool statusChanged) =>
        statusChanged && isActive
            ? $"RxIntel - Your Pharmacy Account Has Been Activated: {pharmacyName}"
            : statusChanged && !isActive
                ? $"RxIntel - Your Pharmacy Account Has Been Deactivated: {pharmacyName}"
                : $"RxIntel - Your Pharmacy Account Has Been Updated: {pharmacyName}";

    public static string BuildUpdateHtmlBody(string businessTradeName, bool isActive, bool statusChanged)
    {
        var statusText = isActive ? "Active" : "Inactive";
        var intro = statusChanged switch
        {
            true when isActive =>
                "Your pharmacy account has been activated by an RxIntel administrator. You may now sign in to RxIntel.",
            true when !isActive =>
                "Your pharmacy account has been deactivated by an RxIntel administrator. If you believe this is an error, please contact RxIntel support.",
            _ =>
                "Your pharmacy account information has been updated by an RxIntel administrator."
        };

        var sb = EmailHtmlBuilder.StartDocument();
        EmailHtmlBuilder.AppendGreeting(sb, businessTradeName);
        EmailHtmlBuilder.AppendParagraph(sb, intro);
        EmailHtmlBuilder.AppendSection(sb, "Account Details", table =>
        {
            EmailHtmlBuilder.AppendRow(table, "BusinessTradeName", businessTradeName);
            EmailHtmlBuilder.AppendRow(table, "Account Status", statusText);
        });
        return EmailHtmlBuilder.Finish(sb);
    }
}
