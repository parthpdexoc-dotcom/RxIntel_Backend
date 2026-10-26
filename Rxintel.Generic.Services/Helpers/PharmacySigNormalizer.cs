using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rxintel.Generic.Services.Helpers
{
    /// <summary>
    /// USA Pharmacy Sig (Directions for Use) Normalizer &amp; Semantic Equivalence Engine.
    /// Pure algorithmic engine that normalizes and evaluates clinical equivalence using
    /// dynamic rules loaded directly from the ErxSigMapping database table.
    /// </summary>
    public static class PharmacySigNormalizer
    {
        // Dynamic in-memory mappings loaded from ErxSigMapping database table
        private static Dictionary<string, string> _dynamicTokens = new(StringComparer.OrdinalIgnoreCase);
        private static (string Pattern, string Replacement)[] _dynamicPhrases = Array.Empty<(string, string)>();
        private static string[] _dynamicFrequencies = new[]
        {
            "at bedtime", "twice daily", "three times daily", "four times daily",
            "daily", "every other day", "every 4 hours", "every 6 hours", "every 8 hours", "every 12 hours"
        };

        private static readonly Dictionary<string, string> BaselineTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            { "po", "by mouth" },
            { "orally", "by mouth" },
            { "oral", "by mouth" },
            { "hr", "hours" },
            { "hrs", "hours" },
            { "q", "every" },
            { "qd", "daily" },
            { "bid", "twice daily" },
            { "tid", "three times daily" },
            { "qid", "four times daily" },
            { "qhs", "at bedtime" },
            { "prn", "as needed" },
            { "tab", "tablet" },
            { "tabs", "tablet" },
            { "cap", "capsule" },
            { "caps", "capsule" },
            { "actuation", "puff" },
            { "actuations", "puff" },
            { "spray", "puff" },
            { "sprays", "puff" },
            { "inhalation", "inhalation" },
            { "inhalations", "puff" },
            { "inhaler", "inhaler" },
            { "gtt", "drop" },
            { "gtts", "drop" },
            { "od", "right eye" },
            { "os", "left eye" },
            { "ou", "both eyes" },
            { "ad", "right ear" },
            { "as", "left ear" },
            { "au", "both ears" },
            { "aaa", "apply to affected area" },
            { "wf", "with food" },
            { "ac", "before meals" },
            { "pc", "after meals" }
        };

        private static readonly Dictionary<string, string> NumberWords = new(StringComparer.OrdinalIgnoreCase)
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" },
            { "four", "4" },
            { "five", "5" },
            { "six", "6" },
            { "seven", "7" },
            { "eight", "8" },
            { "nine", "9" },
            { "ten", "10" }
        };

        /// <summary>
        /// Updates dynamic in-memory mappings loaded from ErxSigMapping table.
        /// Called automatically by ErxSigMappingService upon startup, cache reload, or CRUD changes.
        /// </summary>
        public static void UpdateDynamicMappings(
            Dictionary<string, string> dynamicTokens,
            List<(string Abbr, string Expansion)> dynamicPhrases,
            IEnumerable<string>? dynamicFrequencies = null)
        {
            if (dynamicTokens != null)
            {
                _dynamicTokens = new Dictionary<string, string>(dynamicTokens, StringComparer.OrdinalIgnoreCase);
            }

            if (dynamicPhrases != null)
            {
                _dynamicPhrases = dynamicPhrases.Select(p => (
                    @"(\b" + Regex.Escape(p.Abbr).Replace(@"\ ", @"\s+").Replace(@"\/", @"\s*/\s*") + @"\b)",
                    p.Expansion
                )).ToArray();
            }

            if (dynamicFrequencies != null)
            {
                var freqs = dynamicFrequencies.Where(f => !string.IsNullOrWhiteSpace(f)).Distinct().ToArray();
                if (freqs.Length > 0)
                {
                    _dynamicFrequencies = freqs;
                }
            }
        }

        /// <summary>
        /// Expands abbreviations, normalizes phrasing, and cleans punctuation in a Sig string
        /// using dynamic rules from the database.
        /// e.g. "take 1 tab po qhs" -> "take 1 tablet by mouth at bedtime"
        /// </summary>
        public static string NormalizeSig(string? sig)
        {
            if (string.IsNullOrWhiteSpace(sig))
                return string.Empty;

            var text = sig.Trim().ToLowerInvariant();

            // Pre-clean standard Latin dotted abbreviations before removing punctuation (p.o. -> po, q.h.s. -> qhs)
            text = text.Replace("p.o.", "po")
                       .Replace("p.o", "po")
                       .Replace("q.h.s.", "qhs")
                       .Replace("q.h.s", "qhs")
                       .Replace("q.d.", "qd")
                       .Replace("q.d", "qd")
                       .Replace("b.i.d.", "bid")
                       .Replace("b.i.d", "bid")
                       .Replace("t.i.d.", "tid")
                       .Replace("t.i.d", "tid")
                       .Replace("q.i.d.", "qid")
                       .Replace("q.i.d", "qid")
                       .Replace("p.r.n.", "prn")
                       .Replace("p.r.n", "prn")
                       .Replace("w.f.", "wf")
                       .Replace("w.f", "wf")
                       .Replace("a.c.", "ac")
                       .Replace("a.c", "ac")
                       .Replace("p.c.", "pc")
                       .Replace("p.c", "pc");

            // Pre-clean Latin bar notations for with/without (c̄ -> with, s̄ -> without)
            text = Regex.Replace(text, @"\bc̄\b", "with", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b/c\b", "with", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bs̄\b", "without", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b/s\b", "without", RegexOptions.IgnoreCase);

            // Replace backslashes with slashes (e.g. 140mg\ml -> 140mg/ml)
            text = text.Replace('\\', '/');

            // Normalize concentration notation for single-dose injectables (e.g. 140mg/ml -> 140 mg)
            text = Regex.Replace(text, @"\b(\d+)\s*mg\s*/\s*ml\b", "$1 mg", RegexOptions.IgnoreCase);

            // Normalize common doctor clerical unit entry for autoinjectors (e.g. "inject 140 ml" -> "inject 140 mg")
            text = Regex.Replace(text, @"\b(inject\s+\d+)\s*ml\b", "$1 mg", RegexOptions.IgnoreCase);

            // Pre-clean incidental duration tags commonly appended in e-prescriptions (e.g. "30 days", "for 30 days", "x 30 days")
            text = Regex.Replace(text, @"\b(?:for\s+|x\s*)?\d+\s+days?$", "", RegexOptions.IgnoreCase).Trim();

            // Pre-clean oral inhalation phrasing
            text = Regex.Replace(text, @"\bvia\s+oral\s+inhalation\b", "by inhalation", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\boral\s+inhalation\b", "by inhalation", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\binto\s+(?:the\s+)?lungs\b", "by inhalation", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\binto\s+lungs\b", "by inhalation", RegexOptions.IgnoreCase);

            // Pre-clean frequency phrasing
            text = Regex.Replace(text, @"\b(?:every\s+)?morning\s+and\s+evening\b", "twice daily", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bin\s+the\s+morning\s+and\s+evening\b", "twice daily", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bmorning\s+and\s+night\b", "twice daily", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bevery\s+12\s+hours\b", "twice daily", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bevery\s+8\s+hours\b", "three times daily", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bevery\s+6\s+hours\b", "four times daily", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bevery\s+24\s+hours\b", "daily", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bonce\s+daily\b", "daily", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bonce\s+a\s+day\b", "daily", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\btwice\s+a\s+day\b", "twice daily", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b2\s+times\s+(?:a|per)\s+day\b", "twice daily", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bthree\s+times\s+a\s+day\b", "three times daily", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b3\s+times\s+(?:a|per)\s+day\b", "three times daily", RegexOptions.IgnoreCase);

            // Pre-clean indication qualifiers that do not affect dosing schedule
            text = Regex.Replace(text, @"\bfor\s+(?:wheezing\s+prevention|symptoms|wheezing|asthma|shortness\s+of\s+breath|chest\s+tightness)\b", "", RegexOptions.IgnoreCase);

            // 1. Apply dynamic multi-word phrases from database
            foreach (var (pattern, replacement) in _dynamicPhrases)
            {
                text = Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase);
            }

            // 2. Remove punctuation that does not contribute to clinical meaning
            text = Regex.Replace(text, @"[.,;:!?()\[\]""/\\]", " ");

            // 3. Token-level normalization
            var tokens = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var normalizedTokens = new List<string>(tokens.Length);

            foreach (var token in tokens)
            {
                // Dynamic database token expansion
                if (_dynamicTokens.TryGetValue(token, out var dynamicExp))
                {
                    normalizedTokens.Add(dynamicExp);
                }
                // Standard baseline token expansions
                else if (BaselineTokens.TryGetValue(token, out var baseExp))
                {
                    normalizedTokens.Add(baseExp);
                }
                // Number words: "one" -> "1", "two" -> "2"
                else if (NumberWords.TryGetValue(token, out var num))
                {
                    normalizedTokens.Add(num);
                }
                else
                {
                    // Plural forms like tablets -> tablet, capsules -> capsule, drops -> drop, puffs -> puff
                    if (token.EndsWith("tablets", StringComparison.OrdinalIgnoreCase))
                        normalizedTokens.Add("tablet");
                    else if (token.EndsWith("capsules", StringComparison.OrdinalIgnoreCase))
                        normalizedTokens.Add("capsule");
                    else if (token.EndsWith("drops", StringComparison.OrdinalIgnoreCase))
                        normalizedTokens.Add("drop");
                    else if (token.EndsWith("puffs", StringComparison.OrdinalIgnoreCase))
                        normalizedTokens.Add("puff");
                    else
                        normalizedTokens.Add(token);
                }
            }

            var result = string.Join(" ", normalizedTokens);

            // Run dynamic phrases once more in case token substitutions generated recognized phrases
            foreach (var (pattern, replacement) in _dynamicPhrases)
            {
                result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);
            }

            // Collapse multiple spaces
            return Regex.Replace(result.Trim(), @"\s+", " ");
        }

        /// <summary>
        /// Translates a raw abbreviated Sig into a clean, capitalized Simple English sentence.
        /// e.g. "take 1 tab po qhs" -> "Take 1 tablet by mouth at bedtime"
        /// </summary>
        public static string ToSimpleEnglishSentence(string? sig)
        {
            if (string.IsNullOrWhiteSpace(sig))
                return string.Empty;

            var normalized = NormalizeSig(sig);
            if (string.IsNullOrEmpty(normalized))
                return string.Empty;

            // Capitalize first character
            return char.ToUpperInvariant(normalized[0]) + normalized.Substring(1);
        }

        /// <summary>
        /// Determines whether two Sig instructions are clinically equivalent.
        /// Handles:
        /// 1. Exact or case-insensitive string equality.
        /// 2. Normalized abbreviation expansion (e.g. "take 1 tab po qhs" == "TAKE 1 TAB BY MOUTH AT BEDTIME").
        /// 3. Subsets (e.g. ERX is "By mouth (orally)" and MM is "TAKE 1 TAB BY MOUTH AT BEDTIME").
        /// 4. Token-set equivalence where order differs slightly without clinical conflict.
        /// </summary>
        public static bool AreSigsEquivalent(string? sig1, string? sig2)
        {
            // Both empty/null -> match
            if (string.IsNullOrWhiteSpace(sig1) && string.IsNullOrWhiteSpace(sig2))
                return true;

            // One empty, one not -> discrepancy
            if (string.IsNullOrWhiteSpace(sig1) || string.IsNullOrWhiteSpace(sig2))
                return false;

            // Direct case-insensitive match
            if (string.Equals(sig1.Trim(), sig2.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            // Check Muse AI Continuous Learning Memory
            if (Rxintel.Generic.Services.MetaAI.AiSigKnowledgeMemory.TryGetLearnedEquivalence(sig1, sig2, out var learnedResult))
            {
                if (learnedResult != null && learnedResult.IsCompliant)
                    return true;
            }

            var norm1 = NormalizeSig(sig1);
            var norm2 = NormalizeSig(sig2);

            // Normalized match
            if (string.Equals(norm1, norm2, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check if one is a subset of the other without conflicting clinical elements
            if (IsCompatibleSubset(norm1, norm2) || IsCompatibleSubset(norm2, norm1))
                return true;

            // Token overlap score: If 75%+ of significant tokens match and frequencies/routes/quantities do not conflict
            if (HaveMatchingCoreElements(norm1, norm2))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if a shorter Sig is a compatible subset of a longer Sig.
        /// e.g. Shorter = "by mouth" and Longer = "take 1 tablet by mouth at bedtime"
        /// The shorter provides partial directions (e.g. route only) that are fully satisfied in the longer without conflict.
        /// </summary>
        private static bool IsCompatibleSubset(string shorter, string longer)
        {
            if (string.IsNullOrWhiteSpace(shorter) || string.IsNullOrWhiteSpace(longer))
                return false;

            if (longer.Contains(shorter, StringComparison.OrdinalIgnoreCase))
                return true;

            var shortTokens = shorter.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                     .Where(t => !IsStopWord(t)).ToList();
            var longTokens = longer.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                   .Where(t => !IsStopWord(t)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (shortTokens.Count > 0 && shortTokens.All(t => longTokens.Contains(t)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Verifies that the core components (Dosage/Quantity, Route, Frequency) align without contradictions.
        /// </summary>
        private static bool HaveMatchingCoreElements(string norm1, string norm2)
        {
            // Check for Frequency conflict (e.g. twice daily vs at bedtime)
            var freq1 = ExtractFrequency(norm1);
            var freq2 = ExtractFrequency(norm2);

            if (!string.IsNullOrEmpty(freq1) && !string.IsNullOrEmpty(freq2) && !string.Equals(freq1, freq2, StringComparison.OrdinalIgnoreCase))
            {
                return false; // Real discrepancy in dosing schedule!
            }

            // Check for Quantity conflict (e.g. 1 tablet vs 2 tablets)
            var num1 = ExtractFirstNumber(norm1);
            var num2 = ExtractFirstNumber(norm2);
            if (num1.HasValue && num2.HasValue && num1.Value != num2.Value)
            {
                return false; // Real discrepancy in tablet count!
            }

            // Check token overlap
            var tokens1 = norm1.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => !IsStopWord(t)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var tokens2 = norm2.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => !IsStopWord(t)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (tokens1.Count == 0 || tokens2.Count == 0)
                return false;

            var common = tokens1.Intersect(tokens2).Count();
            var minCount = Math.Min(tokens1.Count, tokens2.Count);

            return (double)common / minCount >= 0.75;
        }

        private static string? ExtractFrequency(string sig)
        {
            foreach (var f in _dynamicFrequencies)
            {
                if (sig.Contains(f, StringComparison.OrdinalIgnoreCase))
                    return f;
            }
            return null;
        }

        private static int? ExtractFirstNumber(string sig)
        {
            var match = Regex.Match(sig, @"\b\d+\b");
            if (match.Success && int.TryParse(match.Value, out var num))
                return num;
            return null;
        }

        private static bool IsStopWord(string word)
        {
            return word is "take" or "use" or "a" or "an" or "the" or "to" or "for" or "with" or "and" or "in" or "at"
                or "only" or "severe" or "mild" or "moderate";
        }
    }
}
