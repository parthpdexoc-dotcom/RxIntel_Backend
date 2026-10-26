using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Helpers;

/// <summary>
/// OptumRx excess (net) uses quantity ordered from VAWD wholesalers only.
/// </summary>
internal static class DataAnalyticsOptumRxHelper
{
    public const string InsuranceName = "OptumRx";

    public static bool IsOptumRx(string? insuranceName) =>
        !string.IsNullOrWhiteSpace(insuranceName)
        && string.Equals(insuranceName.Trim(), InsuranceName, StringComparison.OrdinalIgnoreCase);

    public static decimal SumVawdQuantityOrdered(IEnumerable<Purchase> purchases) =>
        purchases
            .Where(p => p.Wholesaler is { Vawd: true })
            .Sum(PurchaseValidationHelper.GetEffectiveTotalQuantityOrdered);

    public static decimal GetQuantityOrderedBaselineForNet(
        string insuranceName,
        decimal totalQuantityOrdered,
        IReadOnlyList<Purchase> ndcPurchases) =>
        IsOptumRx(insuranceName)
            ? SumVawdQuantityOrdered(ndcPurchases)
            : totalQuantityOrdered;
}
