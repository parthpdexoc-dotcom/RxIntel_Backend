using Rxintel.Generic.Dtos.Dtos;

namespace Rxintel.Generic.Services.Interfaces;

public interface IWholesalerNotificationService
{
    /// <summary>
    /// Emails all platform admins that a pharmacy user added a wholesaler for verification.
    /// </summary>
    Task NotifyAdminsWholesalerAddedByPharmacyAsync(
        Guid pharmacyId,
        WholesalerDto wholesaler,
        string? addedByName,
        string? addedByEmail);

    /// <summary>
    /// Emails all platform admins for VAWD expiry milestones
    /// (1 month / 1 week / 1 day before, and 1 month after).
    /// </summary>
    Task SendDueExpiryRemindersAsync(CancellationToken cancellationToken = default);
}
