using System.Text.RegularExpressions;

namespace Rxintel.Generic.Common.Helpers;

/// <summary>
/// Shared validation utilities used across Billing, Purchase, and other domain helpers.
/// </summary>
public static class ValidationHelperBase
{
    public const int MaxNdcLength = 20;

    /// <summary>
    /// US NDC after hyphen removal: 10-11 digits only
    /// (e.g. 00406012510, or hyphenated input like 00406-0125-10 / 59762-3720-1).
    /// </summary>
    public const string UsNdcPattern = @"^\d{10,11}$";

    /// <summary>Numeric value: 500, 12.5, 0.03, 0.005 — no letters.</summary>
    public const string NumericValuePattern = @"^[0-9]+(\.[0-9]+)?$";

    /// <summary>Standard forbidden chars for general text fields.</summary>
    public static readonly char[] ForbiddenTextChars = ['@', '#', '<', '>', '&', ';', '{', '}'];

    public static bool IsNotEmptyGuid(Guid value) => value != Guid.Empty;

    /// <summary>
    /// Accepts plain 10-11 digit NDCs or hyphenated values that normalize to 10-11 digits.
    /// Hyphenated values are padded to 5-4-2 before the digit check
    /// (e.g. 1234-5678-90 → 01234567890).
    /// </summary>
    public static bool IsValidUsNdc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (trimmed.Length > MaxNdcLength)
            return false;

        var normalized = NormalizeNdc(trimmed);
        return normalized != null && Regex.IsMatch(normalized, UsNdcPattern);
    }

    /// <summary>
    /// Unhyphenated 8-10 digit NDCs cannot be padded locally because the missing
    /// zero's segment is unknown. Those values need an FDA / catalog lookup.
    /// </summary>
    public static bool NeedsFdaNdcLookup(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Contains('-'))
            return false;

        var digits = GetPlainNdcDigits(value);
        return digits is { Length: >= 8 and <= 10 };
    }

    /// <summary>
    /// Digits from an unhyphenated NDC, including Excel values such as 781286810.0.
    /// </summary>
    public static string? GetPlainNdcDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Contains('-'))
            return null;

        if (trimmed.EndsWith(".0", StringComparison.Ordinal)
            && trimmed.Length > 2
            && trimmed[..^2].All(char.IsAsciiDigit))
        {
            trimmed = trimmed[..^2];
        }

        if (trimmed.All(char.IsAsciiDigit))
            return trimmed;

        return null;
    }

    public static bool IsElevenDigitNdc(string? value)
    {
        var normalized = NormalizeNdc(value);
        return normalized != null
            && normalized.Length == 11
            && normalized.All(char.IsAsciiDigit);
    }

    /// <summary>
    /// Hyphenated NDC → 5-4-2 with leading zeros, then stored without hyphens
    /// (e.g. 1234-5678-90 → 01234567890). Plain digit NDCs are returned unchanged.
    /// Returns null when input is null/whitespace.
    /// </summary>
    public static string? NormalizeNdc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (TryPadHyphenatedNdcTo11(value, out var ndc11))
            return ndc11;

        return value.Trim().Replace("-", "", StringComparison.Ordinal);
    }

    /// <summary>
    /// Pads a hyphenated NDC to HIPAA 5-4-2. Does not call FDA.
    /// 123-345-78 → 00123034578, 1234-567-89 → 01234056789,
    /// 1234-5678-90 → 01234567890, 12345-6789-1 → 12345678901.
    /// </summary>
    public static bool TryPadHyphenatedNdcTo11(string? value, out string ndc11)
    {
        ndc11 = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (!trimmed.Contains('-'))
            return false;

        var parts = trimmed.Split('-');
        if (parts.Length != 3
            || parts.Any(part => part.Length == 0 || !part.All(char.IsAsciiDigit))
            || parts[0].Length > 5
            || parts[1].Length > 4
            || parts[2].Length > 2)
        {
            return false;
        }

        var digitCount = parts[0].Length + parts[1].Length + parts[2].Length;
        if (digitCount is < 8 or > 11)
            return false;

        ndc11 = parts[0].PadLeft(5, '0')
            + parts[1].PadLeft(4, '0')
            + parts[2].PadLeft(2, '0');
        return ndc11.Length == 11;
    }

    public static bool ContainsForbiddenChars(string value, char[] forbidden) =>
        value.IndexOfAny(forbidden) >= 0;

    public static bool ContainsPotentialScriptInjection(string value) =>
        Regex.IsMatch(value, @"<\s*script", RegexOptions.IgnoreCase);

    public static bool IsValidNumericValue(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value.Trim(), NumericValuePattern);

    /// <summary>
    /// Generic text field validation: null → trim → length → forbidden chars → script injection → regex.
    /// </summary>
    public static bool IsValidTextField(
        string? value,
        int maxLength,
        char[] forbiddenChars,
        string regexPattern)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
            return false;

        if (ContainsForbiddenChars(trimmed, forbiddenChars))
            return false;

        if (ContainsPotentialScriptInjection(trimmed))
            return false;

        return Regex.IsMatch(trimmed, regexPattern);
    }
}
