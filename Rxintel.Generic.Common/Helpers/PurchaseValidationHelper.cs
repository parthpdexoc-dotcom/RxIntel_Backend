using System.Globalization;
using System.Text.RegularExpressions;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Common.Helpers;

/// <summary>
/// Purchase / wholesaler invoice line validation (NDC, quantity, package size, invoice).
/// </summary>
public static class PurchaseValidationHelper
{
    /// <summary>US NDC — see <see cref="ValidationHelperBase.UsNdcPattern"/>.</summary>
    public const string PurchaseNdcPattern = ValidationHelperBase.UsNdcPattern;

    /// <summary>Integer only package size (e.g. 10, 500, 1200).</summary>
    public const string PackageSizeIntegerPattern = @"^[0-9]+$";

    /// <summary>Letters, digits, hyphen, slash, underscore — must start and end with alphanumeric.</summary>
    public const string InvoiceNumberPattern = @"^[A-Za-z0-9](?:[A-Za-z0-9\-_/]*[A-Za-z0-9])?$";

    public const decimal MaxQuantityPurchased = 999_999_999m;
    public const int MaxPackageSizeLength = 100;
    public const int MaxInvoiceNumberLength = 50;
    public const int MaxInvoiceAgeYears = 10;

    private static readonly char[] ForbiddenPackageSizeChars = ['@', '#', '<', '>', '&', ';', '{', '}'];
    private static readonly char[] ForbiddenInvoiceNumberChars = ['@', '#', '<', '>', '&', ';', '(', ')', '{', '}', ' '];

    public static bool IsNotEmptyGuid(Guid value) => ValidationHelperBase.IsNotEmptyGuid(value);

    public static bool IsValidPurchaseNdc(string? value) => ValidationHelperBase.IsValidUsNdc(value);

    public static bool IsValidQuantityPurchased(decimal value) =>
        value > 0 && value <= MaxQuantityPurchased;

    public static bool IsValidPackageSize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        if (trimmed.Length > MaxPackageSizeLength)
            return false;

        if (ValidationHelperBase.ContainsForbiddenChars(trimmed, ForbiddenPackageSizeChars))
            return false;

        if (ValidationHelperBase.ContainsPotentialScriptInjection(trimmed))
            return false;

        return Regex.IsMatch(trimmed, PackageSizeIntegerPattern);
    }

    public static bool IsValidInvoiceNumber(string? value) =>
        ValidationHelperBase.IsValidTextField(value, MaxInvoiceNumberLength, ForbiddenInvoiceNumberChars, InvoiceNumberPattern);

    /// <summary>Optional invoice date: not in the future, not older than 10 years.</summary>
    public static bool IsValidInvoiceDate(DateTime? value)
    {
        if (!value.HasValue)
            return true;

        var date = value.Value.Date;
        var today = DateTime.Today;
        var oldest = today.AddYears(-MaxInvoiceAgeYears);

        return date <= today && date >= oldest;
    }

    /// <summary>Total units ordered = packages × units per package (e.g. 3 × 100 = 300 tablets).</summary>
    public static decimal CalculateTotalQuantityOrdered(decimal quantityPurchased, string? packageSize)
    {
        if (!TryParsePackageSize(packageSize, out var size))
            return 0m;

        return quantityPurchased * size;
    }

    /// <summary>Uses stored total when present; otherwise derives from quantity × package size.</summary>
    public static decimal GetEffectiveTotalQuantityOrdered(
        decimal quantityPurchased,
        string? packageSize,
        decimal storedTotalQuantityOrdered) =>
        storedTotalQuantityOrdered > 0
            ? storedTotalQuantityOrdered
            : CalculateTotalQuantityOrdered(quantityPurchased, packageSize);

    public static decimal GetEffectiveTotalQuantityOrdered(Purchase purchase) =>
        GetEffectiveTotalQuantityOrdered(
            purchase.QuantityPurchased,
            purchase.PackageSize,
            purchase.TotalQuantityOrdered);

    /// <summary>Parses integer package size (e.g. 100 tablets per package). Returns 0 when invalid.</summary>
    public static decimal ParsePackageSize(string? packageSize) =>
        TryParsePackageSize(packageSize, out var value) ? value : 0m;

    /// <summary>Converts billed/ordered unit quantity into package equivalents.</summary>
    public static decimal UnitsToPackages(decimal units, decimal packageSizeValue) =>
        packageSizeValue > 0 ? units / packageSizeValue : 0m;

    public static decimal UnitsToPackages(decimal units, string? packageSize) =>
        UnitsToPackages(units, ParsePackageSize(packageSize));

    public static bool TryParsePackageSize(string? packageSize, out decimal value)
    {
        value = 0m;
        if (!IsValidPackageSize(packageSize))
            return false;

        return decimal.TryParse(
            packageSize!.Trim(),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out value) && value > 0;
    }
}
