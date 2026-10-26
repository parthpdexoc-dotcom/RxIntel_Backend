namespace Rxintel.Generic.Common.Helpers;

public sealed class FieldMappingEmailLabels
{
    public required string ImportName { get; init; }
    public required string MappingName { get; init; }
    public required string ContextLabel { get; init; }
    public required string ContextDescription { get; init; }
    public required string CompletedMappingDescription { get; init; }
}

public static class FieldMappingEmailHelper
{
    public static readonly FieldMappingEmailLabels Billing = new()
    {
        ImportName = "Billing",
        MappingName = "Billing Field Mapping",
        ContextLabel = "Pharmacy Software",
        ContextDescription = "for their pharmacy software",
        CompletedMappingDescription = "for your pharmacy software"
    };

    public static readonly FieldMappingEmailLabels Purchase = new()
    {
        ImportName = "Purchase",
        MappingName = "Wholesaler Field Mapping",
        ContextLabel = "Wholesaler",
        ContextDescription = "for the selected wholesaler",
        CompletedMappingDescription = "for your purchase upload"
    };

    public static string BuildAdminRequestSubject(FieldMappingEmailLabels labels, string pharmacyName) =>
        $"RxIntel - {labels.ImportName} Field Mapping Required: {pharmacyName}";

    public static string BuildAdminRequestHtmlBody(
        FieldMappingEmailLabels labels,
        string adminName,
        string pharmacyName,
        string contextName,
        string? uploaderEmail)
    {
        var sb = EmailHtmlBuilder.StartDocument();
        EmailHtmlBuilder.AppendGreeting(sb, adminName);
        EmailHtmlBuilder.AppendParagraph(
            sb,
            $"A pharmacy user attempted to upload a {labels.ImportName} CSV, but no {labels.MappingName} exists {labels.ContextDescription}. Please configure the required {labels.MappingName} in RxIntel.");
        EmailHtmlBuilder.AppendSection(sb, "Request Details", table =>
        {
            EmailHtmlBuilder.AppendRow(table, "Pharmacy", pharmacyName);
            EmailHtmlBuilder.AppendRow(table, labels.ContextLabel, contextName);
            EmailHtmlBuilder.AppendRow(table, "User Email", string.IsNullOrWhiteSpace(uploaderEmail) ? "—" : uploaderEmail);
        });
        EmailHtmlBuilder.AppendParagraph(
            sb,
            $"After the {labels.MappingName} is saved, the pharmacy user will be notified automatically and can upload the {labels.ImportName} CSV again.");
        return EmailHtmlBuilder.Finish(sb);
    }

    public static string BuildPharmacyAdminRequestAcknowledgedSubject(FieldMappingEmailLabels labels, string pharmacyName) =>
        $"RxIntel - {labels.ImportName} Field Mapping Request Sent: {pharmacyName}";

    public static string BuildPharmacyAdminRequestAcknowledgedHtmlBody(
        FieldMappingEmailLabels labels,
        string pharmacyAdminName,
        string pharmacyName,
        string contextName,
        string? uploaderEmail)
    {
        var sb = EmailHtmlBuilder.StartDocument();
        EmailHtmlBuilder.AppendGreeting(sb, pharmacyAdminName);
        EmailHtmlBuilder.AppendParagraph(
            sb,
            $"A {labels.ImportName} CSV upload could not proceed because no {labels.MappingName} exists. An RxIntel administrator has been notified and received the mapping request email.");
        EmailHtmlBuilder.AppendSection(sb, "Request Details", table =>
        {
            EmailHtmlBuilder.AppendRow(table, "Pharmacy", pharmacyName);
            EmailHtmlBuilder.AppendRow(table, labels.ContextLabel, contextName);
            EmailHtmlBuilder.AppendRow(table, "Uploader Email", string.IsNullOrWhiteSpace(uploaderEmail) ? "—" : uploaderEmail);
        });
        EmailHtmlBuilder.AppendParagraph(
            sb,
            $"You will be notified when the {labels.MappingName} is configured so the CSV can be uploaded again.");
        return EmailHtmlBuilder.Finish(sb);
    }

    public static string BuildPharmacyUserCompletedSubject(FieldMappingEmailLabels labels, string pharmacyName) =>
        $"RxIntel - {labels.ImportName} Field Mapping Completed: {pharmacyName}";

    public static string BuildPharmacyUserCompletedHtmlBody(
        FieldMappingEmailLabels labels,
        string userName,
        string pharmacyName,
        string contextName)
    {
        var sb = EmailHtmlBuilder.StartDocument();
        EmailHtmlBuilder.AppendGreeting(sb, userName);
        EmailHtmlBuilder.AppendParagraph(
            sb,
            $"The required {labels.MappingName} {labels.CompletedMappingDescription} has been configured by an RxIntel administrator.");
        EmailHtmlBuilder.AppendSection(sb, "Mapping Details", table =>
        {
            EmailHtmlBuilder.AppendRow(table, "Pharmacy", pharmacyName);
            EmailHtmlBuilder.AppendRow(table, labels.ContextLabel, contextName);
        });
        EmailHtmlBuilder.AppendParagraph(
            sb,
            $"You can now sign in to RxIntel and upload your {labels.ImportName} CSV again.");
        return EmailHtmlBuilder.Finish(sb);
    }
}
