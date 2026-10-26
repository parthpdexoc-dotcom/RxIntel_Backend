using System.Text.RegularExpressions;

namespace Rxintel.Generic.Common.Helpers;

public static class PharmacyValidationHelper
{
    public const string AlphanumericPattern = @"^[A-Za-z0-9]+$";
    public const string DeaPattern = @"^[A-Za-z]{2}\d{7}$";
    public const string PharmacyPasswordPattern = @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$";
    public const string UserIdPattern = @"^[a-zA-Z0-9._-]+$";
    public const string LettersAndSpacesPattern = @"^[A-Za-z\s]+$";
    public const string TenDigitsPattern = @"^\d{10}$";
    public const string ThirteenDigitsPattern = @"^\d{13}$";
    public const string ZipPattern = @"^\d{5}$";
    /// <summary>Legacy default; prefer <see cref="StateBoardLicenseRules"/>.</summary>
    public const string StateBoardLicensePattern = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z0-9\-]{5,20}$";
    public const string UsStateCodePattern = @"^[A-Z]{2}$";

    /// <summary>
    /// Practical email format: local part allows letters, digits, . _ + - only;
    /// domain allows letters, digits, . - ; TLD at least 2 letters.
    /// Rejects $ # % & and other special characters.
    /// </summary>
    public const string PharmacyEmailPattern = @"^[a-zA-Z0-9._+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    public const string InsuranceNamePattern = @"^[a-zA-Z0-9\s.&'()-]+$";
    public const string AddressPattern = @"^[A-Za-z0-9\s,./#-]+$";
    public const string BusinessNamePattern = @"^[A-Za-z0-9\s.&'()-]+$";
    public const string CityPattern = @"^[A-Za-z\s'-]+$";
    public const string PersonNamePattern = @"^[A-Za-z\s'-]+$";
    public const string ContactTitlePattern = @"^[A-Za-z0-9\s'-]+$";
    public const string SoftwareSystemPattern = @"^[A-Za-z0-9\s./-]+$";
    public const string BinNumberPattern = @"^\d{6}$";

    /// <summary>US NDC — see <see cref="ValidationHelperBase.UsNdcPattern"/>.</summary>
    public const string NdcPattern = ValidationHelperBase.UsNdcPattern;

    /// <summary>
    /// Drug names as pharmacy systems export them. Backslash and semicolon are allowed because
    /// combination products use them as ingredient separators (HYDROCODONE\APAP, 110MCG\ACTUATION).
    /// </summary>
    public const string DrugNamePattern = @"^[a-zA-Z0-9\s./()%',&+;\\-]+$";
    public const string MedicineOptionalTextPattern = @"^[a-zA-Z0-9\s.%/+-]+$";
    public const string MedicineFormPattern = @"^[a-zA-Z\s]+$";

    /// <summary>
    /// Unit / strength code only (e.g. MG, MCG, ML, EA, TAB, %). No digits � not values like ML123.
    /// </summary>
    public const string MedicineUnitCodePattern = @"^[a-zA-Z%]+$";

    /// <summary>
    /// Numeric amount only (e.g. 500, 12.5, 0.03, 0.005). No letters.
    /// </summary>
    public const string MedicineNumericValuePattern = ValidationHelperBase.NumericValuePattern;

    public static bool IsAlphanumericOnly(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, AlphanumericPattern);

    public static bool HasValidDeaFormat(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value.Trim(), DeaPattern);

    public static bool HasValidDeaSecondLetter(string? dea, string? businessTradeName)
    {
        if (!HasValidDeaFormat(dea) || string.IsNullOrWhiteSpace(businessTradeName))
            return false;

        var expectedLetter = GetFirstLetter(businessTradeName);
        if (!expectedLetter.HasValue)
            return false;

        return char.ToUpperInvariant(dea!.Trim()[1]) == expectedLetter.Value;
    }

    public static bool HasValidDeaChecksum(string? dea)
    {
        if (!HasValidDeaFormat(dea))
            return false;

        var digits = dea!.Trim()[2..];
        var d1 = digits[0] - '0';
        var d2 = digits[1] - '0';
        var d3 = digits[2] - '0';
        var d4 = digits[3] - '0';
        var d5 = digits[4] - '0';
        var d6 = digits[5] - '0';
        var d7 = digits[6] - '0';

        var sumOddPositions = d1 + d3 + d5;
        var sumEvenPositions = (d2 + d4 + d6) * 2;
        var checkDigit = (sumOddPositions + sumEvenPositions) % 10;

        return checkDigit == d7;
    }

    private static char? GetFirstLetter(string value)
    {
        foreach (var character in value.Trim())
        {
            if (char.IsLetter(character))
                return char.ToUpperInvariant(character);
        }

        return null;
    }

    public static bool IsValidPharmacyPassword(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, PharmacyPasswordPattern);

    public static bool IsValidUserId(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, UserIdPattern);

    public static bool IsLettersAndSpacesOnly(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, LettersAndSpacesPattern);

    public static bool IsTenDigitsOnly(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, TenDigitsPattern);

    public static bool IsThirteenDigitsOnly(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, ThirteenDigitsPattern);

    public static bool IsValidZip(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, ZipPattern);

    public static bool IsValidStateBoardLicenseNumber(string? value) =>
        IsValidStateBoardLicenseNumber(value, null);

    public static bool IsValidStateBoardLicenseNumber(string? value, string? stateBoardLicenseRuleId) =>
        StateBoardLicenseRules.IsValid(value, stateBoardLicenseRuleId);

    public static string GetStateBoardLicenseValidationMessage(string? stateBoardLicenseRuleId) =>
        StateBoardLicenseRules.GetValidationMessage(stateBoardLicenseRuleId);

    public static string SanitizeStateBoardLicenseInput(
        string? value,
        string? stateBoardLicenseRuleId) =>
        StateBoardLicenseRules.SanitizeInput(value, stateBoardLicenseRuleId);

    public static bool IsValidUsStateCode(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, UsStateCodePattern);

    public static bool IsValidPharmacyEmail(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, PharmacyEmailPattern);

    public static bool IsValidInsuranceName(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, InsuranceNamePattern);

    public static bool IsValidAddress(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        Regex.IsMatch(value.Trim(), AddressPattern);

    public static bool IsValidBusinessName(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        Regex.IsMatch(value.Trim(), BusinessNamePattern);

    public static bool IsValidCity(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        Regex.IsMatch(value.Trim(), CityPattern);

    public static bool IsValidPersonName(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        Regex.IsMatch(value.Trim(), PersonNamePattern);

    public static bool IsValidContactTitle(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        Regex.IsMatch(value.Trim(), ContactTitlePattern);

    public static bool IsValidSoftwareSystem(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        Regex.IsMatch(value.Trim(), SoftwareSystemPattern);

    public static bool IsValidSoftwareName(string? value) => IsValidSoftwareSystem(value);

    public static bool IsValidWholesalerName(string? value) => IsValidInsuranceName(value);

    public static bool IsValidBinNumber(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && Regex.IsMatch(value.Trim(), BinNumberPattern);

    public static bool IsValidNdc(string? value) => ValidationHelperBase.IsValidUsNdc(value);

    public static bool IsValidDrugName(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, DrugNamePattern);

    public static bool IsValidMedicineOptionalText(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, MedicineOptionalTextPattern);

    public static bool IsValidMedicineForm(string? value) =>
        MedicineValidationHelper.IsValidMedicineDosageForm(value);

    public static bool IsValidMedicineUnitCode(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, MedicineUnitCodePattern);

    public static bool IsValidMedicineUnitType(string? value) => IsValidMedicineUnitCode(value);

    public static bool IsValidMedicineNumericValue(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, MedicineNumericValuePattern);
}
