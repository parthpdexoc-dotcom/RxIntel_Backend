using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Helpers;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services;

public class WholesalerFieldMappingNotificationService
    : FieldMappingNotificationServiceBase, IWholesalerFieldMappingNotificationService
{
    private readonly IPharmacyWholesalerRepository _pharmacyWholesalerRepository;
    private readonly IWholesalerRepository _wholesalerRepository;

    public WholesalerFieldMappingNotificationService(
        IEmailService emailService,
        IAdminRepository adminRepository,
        IPharmacyRepository pharmacyRepository,
        IPharmacyUserRepository pharmacyUserRepository,
        IPharmacyWholesalerRepository pharmacyWholesalerRepository,
        IWholesalerRepository wholesalerRepository,
        ILogger<WholesalerFieldMappingNotificationService> logger)
        : base(emailService, adminRepository, pharmacyRepository, pharmacyUserRepository, logger)
    {
        _pharmacyWholesalerRepository = pharmacyWholesalerRepository;
        _wholesalerRepository = wholesalerRepository;
    }

    protected override FieldMappingEmailLabels Labels => FieldMappingEmailHelper.Purchase;

    protected override string NotificationArea => "Wholesaler field mapping";

    public async Task NotifyAdminsMappingMissingAsync(
        Guid pharmacyId,
        Guid wholesalerId,
        string? uploaderName,
        string? uploaderEmail)
    {
        var wholesalerName = await ResolveWholesalerNameAsync(wholesalerId);
        await NotifyMappingMissingAsync(pharmacyId, wholesalerName, uploaderEmail);
    }

    public async Task NotifyPharmacyUsersMappingConfiguredAsync(Guid wholesalerId)
    {
        if (wholesalerId == Guid.Empty)
            return;

        var wholesalerName = await ResolveWholesalerNameAsync(wholesalerId);
        var pharmacies = await GetPharmaciesForWholesalerAsync(wholesalerId);

        await NotifyPharmacyUsersMappingConfiguredAsync(
            wholesalerName,
            pharmacies,
            $"Wholesaler field mapping completion email skipped because no pharmacies use wholesaler {wholesalerId}.",
            $"Wholesaler field mapping completion email skipped because no active pharmacy users were found for wholesaler {wholesalerId}.");
    }

    private async Task<string> ResolveWholesalerNameAsync(Guid wholesalerId)
    {
        if (wholesalerId == Guid.Empty)
            return "Unknown Wholesaler";

        var wholesaler = await _wholesalerRepository.GetByIdAsync(wholesalerId, "Id");
        return string.IsNullOrWhiteSpace(wholesaler?.WholesalerName)
            ? "Unknown Wholesaler"
            : wholesaler.WholesalerName.Trim();
    }

    private async Task<List<Pharmacy>> GetPharmaciesForWholesalerAsync(Guid wholesalerId)
    {
        var pharmacyIds = (await _pharmacyWholesalerRepository.GetAllAsync(pw => pw.WholesalerId == wholesalerId))
            .Select(pw => pw.PharmacyId)
            .Distinct()
            .ToList();

        if (pharmacyIds.Count == 0)
            return [];

        var pharmacies = new List<Pharmacy>();
        foreach (var pharmacyId in pharmacyIds)
        {
            var pharmacy = await PharmacyRepository.GetByIdAsync(pharmacyId, "Id");
            if (pharmacy != null)
                pharmacies.Add(pharmacy);
        }

        return pharmacies;
    }
}
