using System.Globalization;
using System.Text.RegularExpressions;
using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Common.Helpers;

/// <summary>Medicine master validation for numeric fields and enum label checks.</summary>
public static class MedicineValidationHelper
{
    public const string NumericValuePattern = ValidationHelperBase.NumericValuePattern;
    public const string StrengthNumericValuePattern = @"^([0-9]+(\.[0-9]+)?|\.[0-9]+)$";

    public const string GenericPattern = @"^[a-zA-Z0-9\s.,()/%'+-]+$";
    public const string RoutePattern = @"^[a-zA-Z\s();]+$";
    public const string LabelerPattern = @"^[a-zA-Z0-9\s.,&()'/_:-]+$";
    public const string SuffixPattern = @"^[a-zA-Z0-9\s.,()/%-]+$";

    public static bool IsValidNumericValue(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value.Trim(), NumericValuePattern);

    public static bool IsValidGeneric(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, GenericPattern);

    public static bool IsValidRoute(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, RoutePattern);

    public static bool IsValidLabeler(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, LabelerPattern);

    public static bool IsValidSuffix(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, SuffixPattern);

    /// <summary>
    /// Validates a single numeric or semicolon-separated numeric strength
    /// (e.g. 10, 2; 8; 4.5; 7, .015; .5; .005).
    /// </summary>
    public static bool IsValidStrength(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 && parts.All(part => Regex.IsMatch(part, StrengthNumericValuePattern));
    }

    public static bool IsValidPositiveNumericValue(string? value)
    {
        if (!IsValidNumericValue(value))
            return false;

        return decimal.TryParse(value!.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
               && amount > 0;
    }

    /// <summary>Validates dosage form labels (e.g. TABLET,CREAM) against <see cref="MedicineForm"/>.</summary>
    public static bool IsValidMedicineDosageForm(string? value) =>
        IsValidCommaSeparatedEnumLabels<MedicineForm>(value);

    /// <summary>Validates free-text dosage form values (e.g. billing imports) against <see cref="MedicineForm"/> labels.</summary>
    public static bool IsValidDosageForm(string? value) =>
        IsValidCommaSeparatedEnumLabels<MedicineForm>(value, requireAtLeastOne: true);

    /// <summary>Validates a positive numeric package size (e.g. 45, 45.9, 1200).</summary>
    public static bool IsValidPackageSize(string? value) =>
        IsValidPositiveNumericValue(value);

    /// <summary>Validates comma-separated enum description labels (e.g. OIL,DRUM).</summary>
    public static bool IsValidCommaSeparatedEnumLabels<T>(string? value, bool requireAtLeastOne = false)
        where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return !requireAtLeastOne;

        // Strength units from FDA often use semicolon combos as one label
        // (e.g. "mg/1; mg/1") and UCUM brackets (e.g. "[iU]/mL").
        var normalized = typeof(T) == typeof(UnitStrength)
            ? NormalizeStrengthUnitLabel(value)
            : value.Trim();

        // Prefer exact full-string match for semicolon FDA strength units.
        var validLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (T enumValue in Enum.GetValues(typeof(T)))
            validLabels.Add(EnumHelper.ToStringValue(enumValue));

        if (validLabels.Contains(normalized))
            return true;

        var parts = normalized.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return !requireAtLeastOne;

        return parts.All(part => validLabels.Contains(part));
    }

    /// <summary>FDA/UCUM normalize: [iU]/mL -> iU/mL; collapse semicolon spacing.</summary>
    public static string NormalizeStrengthUnitLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = Regex.Replace(value.Trim(), @"\[([^\]]+)\]", "$1");
        text = Regex.Replace(text, @"\s*;\s*", "; ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }
}
