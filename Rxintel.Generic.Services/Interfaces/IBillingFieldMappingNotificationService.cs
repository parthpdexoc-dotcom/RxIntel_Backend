namespace Rxintel.Generic.Services.Interfaces;

public interface IBillingFieldMappingNotificationService
{
    Task NotifyAdminsMappingMissingAsync(
        Guid pharmacyId,
        Guid pharmacySoftwareId,
        string? uploaderName,
        string? uploaderEmail);

    Task NotifyPharmacyUsersMappingConfiguredAsync(Guid pharmacySoftwareId);
}
