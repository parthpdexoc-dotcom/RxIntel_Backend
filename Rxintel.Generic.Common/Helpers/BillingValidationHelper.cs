using System.Text.RegularExpressions;

namespace Rxintel.Generic.Common.Helpers;

/// <summary>
/// Pharmacy billing / claim line validation (NDC, RX, drug, insurance amounts).
/// </summary>
public static class BillingValidationHelper
{
    /// <summary>RX10001, RX-2025-01, 123456</summary>
    public const string RxNoPattern = @"^[A-Za-z0-9]([A-Za-z0-9\-]*[A-Za-z0-9])?$";

    /// <summary>Type 2 Diabetes, HIV-1 Therapy, B12 Therapy, COVID-19</summary>
    public const string DrugTherapyPattern = @"^[A-Za-z0-9][A-Za-z0-9\s\-'\./]*$";

    /// <summary>Pharmacy claim BIN — exactly 6 digits (leading zeros allowed).</summary>
    public const string SixDigitBinPattern = @"^\d{6}$";

    public const decimal MaxQuantityBilled = 999_999_999m;
    public const decimal MaxPaidAmount = 9_999_999.99m;
    public const int MaxRxNoLength = 50;
    public const int MaxDrugNameLength = 200;
    public const int MaxDrugFormLength = 100;
    public const int MaxPackageSizeLength = 50;
    public const int MaxDrugTherapyLength = 200;
    public const int MaxInsuranceProviderLength = 100;
    public const int BinLength = 6;
    public const int MaxBillDateAgeYears = 10;
    public const decimal AmountSumTolerance = 0.01m;

    public static bool IsNotEmptyGuid(Guid value) => ValidationHelperBase.IsNotEmptyGuid(value);

    public static bool IsValidBillingNdc(string? value) =>
        PurchaseValidationHelper.IsValidPurchaseNdc(value);

    public static bool IsValidQuantityBilled(decimal value) =>
        value > 0 && value <= MaxQuantityBilled;

    public static bool IsValidRxNo(string? value) =>
        ValidationHelperBase.IsValidTextField(value, MaxRxNoLength, ValidationHelperBase.ForbiddenTextChars, RxNoPattern);

    public static bool IsValidDrugName(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Trim().Length <= MaxDrugNameLength
        && PharmacyValidationHelper.IsValidDrugName(value);

    public static bool IsValidDrugForm(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Trim().Length <= MaxDrugFormLength
        && MedicineValidationHelper.IsValidDosageForm(value);

    public static bool IsValidPackageSize(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Trim().Length <= MaxPackageSizeLength
        && PurchaseValidationHelper.IsValidPackageSize(value);

    public static bool IsValidDrugTherapy(string? value) =>
        ValidationHelperBase.IsValidTextField(value, MaxDrugTherapyLength, ValidationHelperBase.ForbiddenTextChars, DrugTherapyPattern);

    public static bool IsValidDays(int? value)
    {
        if (!value.HasValue)
            return true;

        return value.Value > 0 && value.Value <= 365;
    }

    public static bool IsValidBillDate(DateTime value)
    {
        var date = value.Date;
        var today = DateTime.Today;
        var oldest = today.AddYears(-MaxBillDateAgeYears);
        return date <= today && date >= oldest;
    }

    /// <summary>Currency amount: non-negative, max cap, at most 2 decimal places.</summary>
    public static bool IsValidPaidAmount(decimal value) =>
        value >= 0
        && value <= MaxPaidAmount
        && HasAtMostTwoDecimalPlaces(value);

    public static bool IsValidInsuranceProviderName(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Trim().Length <= MaxInsuranceProviderLength
        && PharmacyValidationHelper.IsValidInsuranceName(value);

    public static bool IsValidSixDigitBin(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && Regex.IsMatch(value.Trim(), SixDigitBinPattern);

    /// <summary>Total paid = primary + secondary (claim totals).</summary>
    public static decimal CalculateTotalInsurancePaidAmount(
        decimal primaryInsurancePaidAmount,
        decimal? secondaryInsurancePaidAmount) =>
        decimal.Round(
            primaryInsurancePaidAmount + (secondaryInsurancePaidAmount ?? 0),
            2);

    /// <summary>Total paid should match primary + secondary (claim totals).</summary>
    public static bool IsValidTotalInsurancePaidAmount(
        decimal primary,
        decimal secondary,
        decimal total) =>
        IsValidPaidAmount(total)
        && Math.Abs(total - (primary + secondary)) <= AmountSumTolerance;

    public static bool HasAtMostTwoDecimalPlaces(decimal value) =>
        decimal.Round(value, 2) == value;
}
