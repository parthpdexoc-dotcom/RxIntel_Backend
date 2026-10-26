using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rxintel.Generic.Services.Helpers
{
    /// <summary>
    /// Clinical Prescription Sig (Directions for Use) PHI/PII De-Identification & Sanitizer.
    /// Ensures strictly zero patient health information (PHI), personally identifiable information (PII),
    /// or user/prescriber metadata is ever transmitted to external AI endpoints (e.g. TypeSafe AI).
    /// </summary>
    public static class ClinicalSigSanitizer
    {
        // 1. Phone numbers: (123) 456-7890, 123-456-7890, +1 123 456 7890, 1234567890
        private static readonly Regex PhoneRegex = new(
            @"(?:\+?1[-.\s]?)?\(?\b[2-9]\d{2}\)?[-.\s]?[2-9]\d{2}[-.\s]?\d{4}\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 2. Dates (DOB, filled date, date stamps): MM/DD/YYYY, MM-DD-YYYY, YYYY-MM-DD, etc.
        private static readonly Regex DateSlashRegex = new(
            @"\b(0?[1-9]|1[0-2])[\/\-.](0?[1-9]|[12]\d|3[01])[\/\-.](19|20)\d{2}\b",
            RegexOptions.Compiled);

        private static readonly Regex DateIsoRegex = new(
            @"\b(19|20)\d{2}[\/\-.](0?[1-9]|1[0-2])[\/\-.](0?[1-9]|[12]\d|3[01])\b",
            RegexOptions.Compiled);

        private static readonly Regex DateWordRegex = new(
            @"\b(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)[a-z]*\s+\d{1,2}(?:st|nd|rd|th)?,?\s+(?:19|20)\d{2}\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex DobPrefixRegex = new(
            @"\b(?:dob|d\.o\.b\.|birth\s*date|date\s*of\s*birth)\s*[:#]?\s*[\w\/\-\.]+",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 3. Social Security Numbers: 000-00-0000
        private static readonly Regex SsnRegex = new(
            @"\b\d{3}[-\s]\d{2}[-\s]\d{4}\b",
            RegexOptions.Compiled);

        // 4. Email addresses
        private static readonly Regex EmailRegex = new(
            @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 5. DEA Numbers: 2 letters + 7 digits (e.g. AB1234567)
        private static readonly Regex DeaRegex = new(
            @"\b[A-Za-z]{2}\d{7}\b",
            RegexOptions.Compiled);

        // 6. NPI Numbers: 10-digit standard provider identifiers starting with 1 or 2
        private static readonly Regex NpiRegex = new(
            @"\b[12]\d{9}\b",
            RegexOptions.Compiled);

        // 7. Prescription / Rx / Script numbers: "Rx# 123456", "Rx: 987654", "Script #12345"
        private static readonly Regex RxNumberRegex = new(
            @"\b(?:rx\s*#?|rxno|rx_num|prescription\s*#?|script\s*#?)\s*[:#]?\s*[A-Za-z0-9\-]+\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 8. Medical Record Numbers / Account IDs: "MRN: 12345", "Acct# 998877"
        private static readonly Regex MrnRegex = new(
            @"\b(?:mrn|patient\s*id|pt\s*id|acct\s*#?|account\s*#?|ref\s*#?)\s*[:#]?\s*[A-Za-z0-9\-]+\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 9. Street addresses with numbers: "123 Main Street", "456 Oak Ave Apt 2B"
        private static readonly Regex StreetAddressRegex = new(
            @"\b\d+\s+[A-Za-z0-9\s,]{3,35}\b(?:street|st|avenue|ave|road|rd|boulevard|blvd|lane|ln|drive|dr|court|ct|way|highway|hwy|suite|ste|apt|apartment)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 10. Patient Name prefix phrases in Sig text (e.g. "Pt: John Doe - take 1 tab...", "For: Jane Smith...")
        private static readonly Regex PatientPrefixRegex = new(
            @"\b(?:patient|pt|for|attn|name)\s*:\s*[A-Za-z\s,.'-]{2,35}(?=(?:;|\s*-\s*|:|\.\s+|take|give|apply|inject|inhale|chew|insert|use|\r|\n|$))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 11. Prescriber / Doctor prefix phrases (e.g. "Dr. Smith:", "Prescriber: John Doe MD")
        private static readonly Regex PrescriberPrefixRegex = new(
            @"\b(?:dr\.?|physician|prescriber|provider|rph)\s*:\s*[A-Za-z\s,.'-]{2,35}(?=(?:;|\s*-\s*|:|\.\s+|take|give|apply|inject|inhale|chew|insert|use|\r|\n|$))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 12. Multiple whitespace / punctuation collapse
        private static readonly Regex MultiSpaceRegex = new(@"\s{2,}", RegexOptions.Compiled);
        private static readonly Regex CleanPunctuationRegex = new(@"^[,\-;:.\s]+|[,\-;:.\s]+$", RegexOptions.Compiled);

        /// <summary>
        /// Sanitizes a prescription direction string by redacting all identifiable user/patient data (PHI/PII),
        /// leaving strictly anonymous clinical dosage, frequency, administration, and duration semantics.
        /// </summary>
        /// <param name="sigText">The raw Sig directions text.</param>
        /// <param name="knownPiiTokens">Optional list of specific patient names, prescriber names, Rx numbers, etc. to explicitly redact.</param>
        /// <returns>A de-identified clinical direction string safe for external AI evaluation.</returns>
        public static string Sanitize(string? sigText, IEnumerable<string?>? knownPiiTokens = null)
        {
            if (string.IsNullOrWhiteSpace(sigText))
                return string.Empty;

            var text = sigText;

            // 1. Explicitly redact known patient/user tokens (Names, DOB, Phones, Addresses, RxNumbers, etc.)
            if (knownPiiTokens != null)
            {
                foreach (var rawToken in knownPiiTokens)
                {
                    if (string.IsNullOrWhiteSpace(rawToken)) continue;
                    var token = rawToken.Trim();
                    // Avoid redacting common clinical short words (e.g. "OR", "IN", "NO", "TAB")
                    if (token.Length < 3) continue;

                    // If token is a full name (e.g. "John Smith" or "Smith, John"), also check individual parts
                    var escaped = Regex.Escape(token);
                    text = Regex.Replace(text, $@"\b{escaped}\b", "", RegexOptions.IgnoreCase);

                    // Also redact individual name parts if length >= 4 (to avoid accidental 2-3 letter words)
                    var parts = token.Split(new[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        if (part.Length >= 4 && !IsClinicalWord(part))
                        {
                            var escapedPart = Regex.Escape(part);
                            text = Regex.Replace(text, $@"\b{escapedPart}\b", "", RegexOptions.IgnoreCase);
                        }
                    }
                }
            }

            // 2. Redact Structured PII (Phone, Dates, SSN, Emails, DEA, NPI, Rx Numbers, MRN, Addresses)
            text = PhoneRegex.Replace(text, "");
            text = DobPrefixRegex.Replace(text, "");
            text = DateSlashRegex.Replace(text, "");
            text = DateIsoRegex.Replace(text, "");
            text = DateWordRegex.Replace(text, "");
            text = SsnRegex.Replace(text, "");
            text = EmailRegex.Replace(text, "");
            text = DeaRegex.Replace(text, "");
            text = NpiRegex.Replace(text, "");
            text = RxNumberRegex.Replace(text, "");
            text = MrnRegex.Replace(text, "");
            text = StreetAddressRegex.Replace(text, "");
            text = PatientPrefixRegex.Replace(text, "");
            text = PrescriberPrefixRegex.Replace(text, "");

            // 3. Normalize whitespace and trailing punctuation
            text = MultiSpaceRegex.Replace(text, " ");
            text = CleanPunctuationRegex.Replace(text, "");

            return text.Trim();
        }

        /// <summary>
        /// Whitelist of clinical/pharmaceutical words that should never be redacted even if a patient/prescriber has a similar name.
        /// </summary>
        private static bool IsClinicalWord(string word)
        {
            var w = word.Trim().ToLowerInvariant();
            return ClinicalWords.Contains(w);
        }

        private static readonly HashSet<string> ClinicalWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "take", "give", "apply", "inject", "inhale", "instill", "insert", "chew", "dissolve",
            "tablet", "tablets", "tab", "tabs", "capsule", "capsules", "cap", "caps",
            "mouth", "oral", "orally", "po", "topical", "subcutaneous", "sq", "sc", "im", "iv",
            "daily", "twice", "three", "four", "times", "hour", "hours", "hrs", "bedtime", "morning", "night",
            "needed", "pain", "severe", "mild", "moderate", "fever", "cough", "meals", "food",
            "every", "other", "days", "weeks", "months", "drops", "gtts", "spray", "sprays",
            "puff", "puffs", "patch", "cream", "ointment", "solution", "suspension", "liquid",
            "dose", "doses", "unit", "units", "milligram", "milligrams", "mg", "ml", "mcg", "gm", "gram"
        };
    }
}
