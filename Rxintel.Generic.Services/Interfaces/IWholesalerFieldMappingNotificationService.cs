namespace Rxintel.Generic.Services.Interfaces;

public interface IWholesalerFieldMappingNotificationService
{
    Task NotifyAdminsMappingMissingAsync(
        Guid pharmacyId,
        Guid wholesalerId,
        string? uploaderName,
        string? uploaderEmail);

    Task NotifyPharmacyUsersMappingConfiguredAsync(Guid wholesalerId);
}
