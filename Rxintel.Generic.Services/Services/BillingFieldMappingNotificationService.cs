using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Helpers;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services;

public class BillingFieldMappingNotificationService
    : FieldMappingNotificationServiceBase, IBillingFieldMappingNotificationService
{
    private readonly IPharmacySoftwareRepository _pharmacySoftwareRepository;

    public BillingFieldMappingNotificationService(
        IEmailService emailService,
        IAdminRepository adminRepository,
        IPharmacyRepository pharmacyRepository,
        IPharmacyUserRepository pharmacyUserRepository,
        IPharmacySoftwareRepository pharmacySoftwareRepository,
        ILogger<BillingFieldMappingNotificationService> logger)
        : base(emailService, adminRepository, pharmacyRepository, pharmacyUserRepository, logger)
    {
        _pharmacySoftwareRepository = pharmacySoftwareRepository;
    }

    protected override FieldMappingEmailLabels Labels => FieldMappingEmailHelper.Billing;

    protected override string NotificationArea => "Billing field mapping";

    public async Task NotifyAdminsMappingMissingAsync(
        Guid pharmacyId,
        Guid pharmacySoftwareId,
        string? uploaderName,
        string? uploaderEmail)
    {
        var pharmacySoftwareName = await ResolvePharmacySoftwareNameAsync(pharmacySoftwareId);
        await NotifyMappingMissingAsync(pharmacyId, pharmacySoftwareName, uploaderEmail);
    }

    public async Task NotifyPharmacyUsersMappingConfiguredAsync(Guid pharmacySoftwareId)
    {
        if (pharmacySoftwareId == Guid.Empty)
            return;

        var pharmacySoftwareName = await ResolvePharmacySoftwareNameAsync(pharmacySoftwareId);
        var pharmacies = await GetPharmaciesForSoftwareAsync(pharmacySoftwareId, pharmacySoftwareName);

        await NotifyPharmacyUsersMappingConfiguredAsync(
            pharmacySoftwareName,
            pharmacies,
            $"Billing field mapping completion email skipped because no pharmacies use software {pharmacySoftwareId}.",
            $"Billing field mapping completion email skipped because no active pharmacy users were found for software {pharmacySoftwareId}.");
    }

    private async Task<string> ResolvePharmacySoftwareNameAsync(Guid pharmacySoftwareId)
    {
        if (pharmacySoftwareId == Guid.Empty)
            return "Unknown Software";

        var software = await _pharmacySoftwareRepository.GetByIdAsync(pharmacySoftwareId, "Id");
        return string.IsNullOrWhiteSpace(software?.SoftwareName)
            ? "Unknown Software"
            : software.SoftwareName.Trim();
    }

    private async Task<List<Pharmacy>> GetPharmaciesForSoftwareAsync(
        Guid pharmacySoftwareId,
        string pharmacySoftwareName)
    {
        var pharmacies = (await PharmacyRepository.GetAllAsync(p => p.PharmacySoftwareId == pharmacySoftwareId))
            .ToList();

        if (string.IsNullOrWhiteSpace(pharmacySoftwareName))
            return pharmacies;

        var normalizedSoftwareName = pharmacySoftwareName.Trim().ToLowerInvariant();
        var pharmaciesByName = await PharmacyRepository.GetAllAsync(p =>
            p.PharmacyBillingSoftwareName != null &&
            p.PharmacyBillingSoftwareName.Trim().ToLower() == normalizedSoftwareName);

        var seenIds = new HashSet<Guid>(pharmacies.Select(p => p.Id));
        foreach (var pharmacy in pharmaciesByName)
        {
            if (seenIds.Add(pharmacy.Id))
                pharmacies.Add(pharmacy);
        }

        return pharmacies;
    }
}
