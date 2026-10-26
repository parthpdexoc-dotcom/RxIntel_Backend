using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Rxintel.Generic.Services.Helpers
{
    /// <summary>
    /// Clinical Pharmacy Sig Dosing Calculator.
    /// Calculates Units Per Day and ERX-Based Days Supply from prescription directions (SIG) and quantity.
    /// 
    /// Formula:
    ///   UnitsPerDay = DoseQuantity * DailyFrequency
    ///   ERX Based Days Supply = ERX Quantity / UnitsPerDay
    /// </summary>
    public static class PharmacySigDosingCalculator
    {
        private static readonly Dictionary<string, decimal> NumberWordMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "half", 0.5m },
            { "one", 1m },
            { "two", 2m },
            { "three", 3m },
            { "four", 4m },
            { "five", 5m },
            { "six", 6m },
            { "seven", 7m },
            { "eight", 8m },
            { "nine", 9m },
            { "ten", 10m },
            { "eleven", 11m },
            { "twelve", 12m }
        };

        /// <summary>
        /// Calculates the number of units (tablets, capsules, puffs, drops, units) consumed per day.
        /// </summary>
        /// <param name="sig">The prescription directions string.</param>
        /// <returns>Units per day, or null if cannot be determined.</returns>
        public static decimal? CalculateUnitsPerDay(string? sig)
        {
            if (string.IsNullOrWhiteSpace(sig))
                return null;

            var cleanSig = sig.Trim().ToLowerInvariant();

            // Special check: Blood glucose testing strips (e.g. "for QID testing", "three times a day testing", "use to test blood glucose 3 times a day")
            var testingMatch = Regex.Match(cleanSig, @"(?:for\s+)?(qid|tid|bid|qd|\d+\s*times?\s*(?:a|per)?\s*day)\s+testing", RegexOptions.IgnoreCase);
            if (testingMatch.Success)
            {
                var freq = ExtractDailyFrequency(testingMatch.Groups[1].Value);
                if (freq.HasValue && freq.Value > 0)
                {
                    return freq.Value; // 1 strip per test * freq
                }
            }

            var dose = ExtractDoseQuantity(cleanSig);
            var frequency = ExtractDailyFrequency(cleanSig);

            if (!frequency.HasValue)
                return null;

            // Default dose to 1.0 if not explicitly mentioned but frequency is specified (e.g., "take daily", "instill into eyes twice daily")
            var effectiveDose = dose ?? 1.0m;

            // Eye drops special check: "both eyes" doubles the dose per administration
            if (Regex.IsMatch(cleanSig, @"\b(?:both\s+eyes|in\s+each\s+eye|ou)\b", RegexOptions.IgnoreCase))
            {
                effectiveDose *= 2.0m;
            }

            return effectiveDose * frequency.Value;
        }

        /// <summary>
        /// Calculates ERX Based Days Supply = Quantity / UnitsPerDay.
        /// </summary>
        /// <param name="quantity">Prescribed or dispensed quantity (numerator).</param>
        /// <param name="sig">Prescription directions string (denominator derivation).</param>
        /// <returns>Whole integer days supply, or null if cannot be calculated.</returns>
        public static int? CalculateDaysSupply(decimal? quantity, string? sig)
        {
            if (!quantity.HasValue || quantity.Value <= 0)
                return null;

            var unitsPerDay = CalculateUnitsPerDay(sig);
            if (!unitsPerDay.HasValue || unitsPerDay.Value <= 0)
                return null;

            var daysSupply = quantity.Value / unitsPerDay.Value;
            return (int)Math.Round(daysSupply, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Calculates the required quantity for a given days supply = DaysSupply * UnitsPerDay.
        /// </summary>
        public static decimal? CalculateRequiredQuantity(int? daysSupply, string? sig)
        {
            if (!daysSupply.HasValue || daysSupply.Value <= 0)
                return null;

            var unitsPerDay = CalculateUnitsPerDay(sig);
            if (!unitsPerDay.HasValue || unitsPerDay.Value <= 0)
                return null;

            return daysSupply.Value * unitsPerDay.Value;
        }

        /// <summary>
        /// Extracts the dose quantity (number of units per administration) from a Sig string.
        /// </summary>
        public static decimal? ExtractDoseQuantity(string? sig)
        {
            if (string.IsNullOrWhiteSpace(sig))
                return null;

            var text = sig.Trim().ToLowerInvariant();

            // 1. Fractions like 1/2
            if (Regex.IsMatch(text, @"\b1/2\b"))
                return 0.5m;
            if (Regex.IsMatch(text, @"\b1/4\b"))
                return 0.25m;
            if (Regex.IsMatch(text, @"\b3/4\b"))
                return 0.75m;

            // 2. Specific dose noun patterns:
            // "take 1 tablet", "take 2 capsules", "inject 15 units", "instill 1 drop", "apply 1 patch"
            var doseNounPattern = @"\b(?:take|instill|inject|inhale|apply|insert|give|use)?\s*(\d+(?:\.\d+)?|\b(?:half|one|two|three|four|five|six|seven|eight|nine|ten)\b)(?:\s*(?:to|-)\s*(\d+(?:\.\d+)?|\b(?:half|one|two|three|four|five|six|seven|eight|nine|ten)\b))?\s*(?:tablets?|capsules?|tabs?|caps?|pills?|puffs?|drops?|gtts?|units?|patches?|patch|syringes?|suppositor(?:y|ies)|appl(?:ication)?)\b";
            var matchNoun = Regex.Match(text, doseNounPattern, RegexOptions.IgnoreCase);
            if (matchNoun.Success)
            {
                // If range (e.g. 1 to 2 tablets), in audit calculation standard max single dose is 2, or take upper bound
                var upper = matchNoun.Groups[2].Value;
                if (!string.IsNullOrEmpty(upper))
                {
                    var valUpper = ParseNumberToken(upper);
                    if (valUpper.HasValue) return valUpper.Value;
                }

                var primary = matchNoun.Groups[1].Value;
                var val = ParseNumberToken(primary);
                if (val.HasValue) return val.Value;
            }

            // 3. Fallback: "take 1 by mouth", "take 2 orally"
            var routePattern = @"\b(?:take|instill|inject|inhale|apply|insert|give|use)\s*(\d+(?:\.\d+)?|\b(?:half|one|two|three|four|five|six|seven|eight|nine|ten)\b)\s*(?:by mouth|po|orally|sublingually|under the tongue|rectally|topically|subcutaneously)?\b";
            var matchRoute = Regex.Match(text, routePattern, RegexOptions.IgnoreCase);
            if (matchRoute.Success)
            {
                var val = ParseNumberToken(matchRoute.Groups[1].Value);
                if (val.HasValue) return val.Value;
            }

            // 4. Check first leading number word if present (e.g. "one tablet daily", "2 tablets twice a day")
            var leadingMatch = Regex.Match(text, @"^(\d+(?:\.\d+)?|\b(?:half|one|two|three|four|five|six|seven|eight|nine|ten)\b)\s*(?:tablets?|capsules?|tabs?|caps?|units?|drops?)", RegexOptions.IgnoreCase);
            if (leadingMatch.Success)
            {
                var val = ParseNumberToken(leadingMatch.Groups[1].Value);
                if (val.HasValue) return val.Value;
            }

            return null;
        }

        /// <summary>
        /// Extracts the daily frequency (number of doses per 24-hour day) from a Sig string.
        /// </summary>
        public static decimal? ExtractDailyFrequency(string? sig)
        {
            if (string.IsNullOrWhiteSpace(sig))
                return null;

            var text = sig.Trim().ToLowerInvariant();

            // Pre-clean standard Latin dotted notations (q.i.d. -> qid, b.i.d. -> bid, etc.)
            text = text.Replace("q.i.d.", "qid")
                       .Replace("t.i.d.", "tid")
                       .Replace("b.i.d.", "bid")
                       .Replace("q.d.", "qd")
                       .Replace("q.h.s.", "qhs")
                       .Replace("q.o.d.", "qod");

            // 1. Every X hours (q4h, q6h, q8h, q12h, every 4 hours, every 6 hrs, etc.)
            var everyHoursMatch = Regex.Match(text, @"\b(?:every|q)\s*(\d+)\s*(?:hours?|hrs?|h)\b", RegexOptions.IgnoreCase);
            if (everyHoursMatch.Success && decimal.TryParse(everyHoursMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var hours) && hours > 0)
            {
                return 24.0m / hours;
            }

            // 2. Multi-Month intervals (e.g., "every three months", "every 3 months", "once every 3 months")
            if (Regex.IsMatch(text, @"\bevery\s+(?:3|three)\s+months?\b|\bevery\s+quarter\b", RegexOptions.IgnoreCase))
            {
                return 1.0m / 90.0m;
            }

            // 3. Monthly (e.g., "once monthly", "once a month", "every month", "once per month")
            if (Regex.IsMatch(text, @"\b(?:once\s+)?(?:monthly|once\s*(?:a|per)\s*month|every\s+month|qm|q30d)\b", RegexOptions.IgnoreCase))
            {
                return 1.0m / 30.0m;
            }

            // 4. Weekly variations
            if (Regex.IsMatch(text, @"\b(?:3|three)\s*times?\s*(?:a|per)?\s*week\b", RegexOptions.IgnoreCase))
            {
                return 3.0m / 7.0m;
            }
            if (Regex.IsMatch(text, @"\b(?:2|two|twice)\s*(?:times?\s*)?(?:a|per)?\s*week\b", RegexOptions.IgnoreCase))
            {
                return 2.0m / 7.0m;
            }
            if (Regex.IsMatch(text, @"\b(?:once\s+)?(?:weekly|once\s*(?:a|per)\s*week|every\s+week|qw|q7d)\b", RegexOptions.IgnoreCase))
            {
                return 1.0m / 7.0m;
            }

            // 5. Every other day (QOD)
            if (Regex.IsMatch(text, @"\b(?:every\s+other\s+day|qod)\b", RegexOptions.IgnoreCase))
            {
                return 0.5m;
            }

            // 6. Four times a day (QID)
            if (Regex.IsMatch(text, @"\b(?:qid|four\s*times?\s*(?:a|per)?\s*day|4\s*times?\s*(?:a|per)?\s*day|4x\s*daily)\b", RegexOptions.IgnoreCase))
            {
                return 4.0m;
            }

            // 7. Three times a day (TID)
            if (Regex.IsMatch(text, @"\b(?:tid|three\s*times?\s*(?:a|per)?\s*day|3\s*times?\s*(?:a|per)?\s*day|3x\s*daily)\b", RegexOptions.IgnoreCase))
            {
                return 3.0m;
            }

            // 8. Twice a day (BID)
            if (Regex.IsMatch(text, @"\b(?:bid|twice\s*(?:a|per)?\s*day|twice\s*daily|2\s*times?\s*(?:a|per)?\s*day|2\s*times?\s*daily|2x\s*daily|morning\s+and\s+(?:night|evening))\b", RegexOptions.IgnoreCase))
            {
                return 2.0m;
            }

            // 9. Once a day (QD, daily, bedtime, morning)
            if (Regex.IsMatch(text, @"\b(?:qd|once\s*(?:a|per)?\s*day|once\s*daily|1\s*time\s*(?:a|per)?\s*day|1\s*time\s*daily|1x\s*daily|daily|every\s+day|each\s+day|qhs|at\s+bedtime|bedtime|nightly|qam|every\s+morning|morning|qpm|every\s+evening|evening|every\s+24\s*hours?|q24h)\b", RegexOptions.IgnoreCase))
            {
                return 1.0m;
            }

            return null;
        }

        private static decimal? ParseNumberToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            token = token.Trim().ToLowerInvariant();

            if (NumberWordMap.TryGetValue(token, out var wordVal))
                return wordVal;

            if (token == "1/2") return 0.5m;
            if (token == "1/4") return 0.25m;
            if (token == "3/4") return 0.75m;

            if (decimal.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                return num;

            return null;
        }
    }
}
