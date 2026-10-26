using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Rxintel.Generic.Common.Helpers;

/// <summary>
/// Rule definitions for state board license numbers. Per-state assignment lives on the <c>State</c> table
/// (<see cref="Entities.Entities.State.StateBoardLicenseRuleId"/>). Keep in sync with frontend <c>stateBoardLicenseRules.ts</c>.
/// </summary>
public static class StateBoardLicenseRules
{
    private const string MsgNum510 =
        "License number must contain numbers only. Length must be between 5 and 10 digits.";
    private const string MsgNum56 =
        "License number must contain numbers only. Length must be 5 or 6 digits.";
    private const string MsgNum6 =
        "License number must be exactly 6 digits (numbers only).";
    private const string MsgNum810 =
        "License number must contain numbers only. Length must be between 8 and 10 digits.";
    private const string MsgPrefix2LD =
        "License number must be 2 letters followed by 4 to 8 digits (e.g. PH12345).";
    private const string MsgAlnumAny =
        "License number may contain letters, numbers, and hyphens. Length must be between 5 and 20 characters.";
    private const string MsgAlnumBoth =
        "License number must contain both letters and numbers and may contain hyphens. Length must be between 5 and 20 characters.";

    private static readonly StateBoardLicenseRuleDefinition DefaultRule = new(
        StateBoardLicenseRuleIds.Num510,
        @"^\d{5,10}$",
        StateBoardLicenseSanitizeMode.DigitsOnly,
        10,
        MsgNum510);

    private static readonly FrozenDictionary<string, StateBoardLicenseRuleDefinition> RulesById =
        new Dictionary<string, StateBoardLicenseRuleDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [StateBoardLicenseRuleIds.Num510] = DefaultRule,
            [StateBoardLicenseRuleIds.Num56] = new(
                StateBoardLicenseRuleIds.Num56,
                @"^\d{5,6}$",
                StateBoardLicenseSanitizeMode.DigitsOnly,
                6,
                MsgNum56),
            [StateBoardLicenseRuleIds.Num6] = new(
                StateBoardLicenseRuleIds.Num6,
                @"^\d{6}$",
                StateBoardLicenseSanitizeMode.DigitsOnly,
                6,
                MsgNum6),
            [StateBoardLicenseRuleIds.Num810] = new(
                StateBoardLicenseRuleIds.Num810,
                @"^\d{8,10}$",
                StateBoardLicenseSanitizeMode.DigitsOnly,
                10,
                MsgNum810),
            [StateBoardLicenseRuleIds.Prefix2LD] = new(
                StateBoardLicenseRuleIds.Prefix2LD,
                @"^[A-Za-z]{2}\d{4,8}$",
                StateBoardLicenseSanitizeMode.LettersAndDigits,
                10,
                MsgPrefix2LD),
            [StateBoardLicenseRuleIds.AlnumAny] = new(
                StateBoardLicenseRuleIds.AlnumAny,
                @"^[A-Za-z0-9-]{5,20}$",
                StateBoardLicenseSanitizeMode.AlphanumericHyphen,
                20,
                MsgAlnumAny),
            [StateBoardLicenseRuleIds.AlnumBoth] = new(
                StateBoardLicenseRuleIds.AlnumBoth,
                @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z0-9-]{5,20}$",
                StateBoardLicenseSanitizeMode.AlphanumericHyphen,
                20,
                MsgAlnumBoth),
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static StateBoardLicenseRuleDefinition GetRuleByRuleId(string? ruleId)
    {
        if (!string.IsNullOrWhiteSpace(ruleId) &&
            RulesById.TryGetValue(ruleId.Trim(), out var rule))
        {
            return rule;
        }

        return DefaultRule;
    }

    public static string SanitizeInput(string? value, string? ruleId)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var rule = GetRuleByRuleId(ruleId);
        var filtered = rule.SanitizeMode switch
        {
            StateBoardLicenseSanitizeMode.DigitsOnly =>
                new string(value.Where(char.IsDigit).ToArray()),
            StateBoardLicenseSanitizeMode.LettersAndDigits =>
                new string(value.Where(char.IsLetterOrDigit).ToArray()),
            _ => new string(value.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray()),
        };

        return filtered.Length <= rule.MaxInputLength
            ? filtered
            : filtered[..rule.MaxInputLength];
    }

    public static bool IsValid(string? value, string? ruleId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var rule = GetRuleByRuleId(ruleId);
        return Regex.IsMatch(value.Trim(), rule.Pattern);
    }

    public static string GetValidationMessage(string? ruleId) =>
        GetRuleByRuleId(ruleId).ErrorMessage;
}

public enum StateBoardLicenseSanitizeMode
{
    DigitsOnly,
    LettersAndDigits,
    AlphanumericHyphen,
}

public sealed record StateBoardLicenseRuleDefinition(
    string RuleId,
    string Pattern,
    StateBoardLicenseSanitizeMode SanitizeMode,
    int MaxInputLength,
    string ErrorMessage);
