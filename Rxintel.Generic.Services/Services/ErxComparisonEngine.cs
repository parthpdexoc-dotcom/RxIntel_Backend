using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Services.Helpers;
using Rxintel.Generic.Services.Interfaces;
using Rxintel.Generic.Services.MetaAI;

namespace Rxintel.Generic.Services.Services
{
    public class ErxComparisonEngine : IErxComparisonEngine
    {
        private readonly ILogger<ErxComparisonEngine> _logger;
        private readonly IDeaScheduleService _deaScheduleService;

        private static readonly string[] UsaDateFormats = new[]
        {
            "M/d/yyyy", "MM/dd/yyyy",
            "M/d/yy", "MM/dd/yy",
            "M-d-yyyy", "MM-dd-yyyy",
            "M/d/yyyy H:mm", "MM/dd/yyyy H:mm",
            "M/d/yyyy HH:mm", "MM/dd/yyyy HH:mm",
            "M/d/yyyy h:mm tt", "MM/dd/yyyy h:mm tt",
            "M/d/yyyy hh:mm tt", "MM/dd/yyyy hh:mm tt",
            "M/d/yyyy H:mm:ss", "MM/dd/yyyy H:mm:ss",
            "M/d/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm:ss",
            "M/d/yyyy h:mm:ss tt", "MM/dd/yyyy h:mm:ss tt",
            "M/d/yyyy hh:mm:ss tt", "MM/dd/yyyy hh:mm:ss tt",
            "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ"
        };
        private static readonly ConcurrentDictionary<string, PropertyInfo?> PropertyCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly RxintelDbContext? _dbContext;
        private readonly ITypeSafeClient? _typeSafeClient;
        private readonly IMetaAiClient? _metaAiClient;
        private readonly IErxReferenceDataService? _referenceDataService;

        public ErxComparisonEngine(
            ILogger<ErxComparisonEngine> logger,
            IDeaScheduleService deaScheduleService,
            ITypeSafeClient? typeSafeClient = null,
            IMetaAiClient? metaAiClient = null,
            RxintelDbContext? dbContext = null,
            IErxReferenceDataService? referenceDataService = null)
        {
            _logger = logger;
            _deaScheduleService = deaScheduleService;
            _typeSafeClient = typeSafeClient;
            _metaAiClient = metaAiClient;
            _dbContext = dbContext;
            _referenceDataService = referenceDataService;
        }

        public List<ErxAuditDiscrepancy> CompareRecord(ErxAuditRecord record, IEnumerable<ErxColumnMatchRule> rules)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (rules == null) return new List<ErxAuditDiscrepancy>();

            var discrepancies = new List<ErxAuditDiscrepancy>();

            foreach (var rule in rules)
            {
                if (!rule.IsActive) continue;
                if (string.Equals(rule.MatchType, "Ignore", StringComparison.OrdinalIgnoreCase)) continue;

                var mmVal = GetPropertyValueAsString(record, rule.MmColumnName);
                var erxVal = GetPropertyValueAsString(record, rule.ErxColumnName);

                var isMatch = EvaluateMatchForRecord(rule, record, mmVal, erxVal, out var customMmVal, out var customErxVal);
                if (!isMatch)
                {
                    var actualMm = customMmVal ?? mmVal;
                    var actualErx = customErxVal ?? erxVal;
                    discrepancies.Add(new ErxAuditDiscrepancy
                    {
                        RecordId = record.RecordId,
                        RuleId = rule.RuleId,
                        MmValue = actualMm,
                        ErxValue = actualErx,
                        Status = "Open",
                        CreatedAt = DateTime.UtcNow
                    });

                    _logger.LogDebug("[ComparisonEngine:CompareRecord] Rx {RxNumber}: Rule '{FieldGroupName}' flagged discrepancy (MM: '{MmVal}' vs ERX: '{ErxVal}').",
                        record.RxNumber, rule.FieldGroupName, actualMm, actualErx);
                }
            }

            if (discrepancies.Count > 0)
            {
                _logger.LogInformation("[ComparisonEngine:CompareRecord] Rx {RxNumber}: Detected {Count} discrepancy(ies).",
                    record.RxNumber, discrepancies.Count);
            }

            return discrepancies;
        }

        public bool EvaluateMatch(ErxColumnMatchRule rule, string? mmValue, string? erxValue)
        {
            return EvaluateMatchForRecord(rule, null, mmValue, erxValue, out _, out _);
        }

        private bool EvaluateMatchForRecord(
            ErxColumnMatchRule rule,
            ErxAuditRecord? record,
            string? mmValue,
            string? erxValue,
            out string? customMmVal,
            out string? customErxVal)
        {
            customMmVal = null;
            customErxVal = null;

            if (rule == null) return true;

            var matchType = rule.MatchType?.Trim() ?? "Exact";
            if (string.Equals(matchType, "Ignore", StringComparison.OrdinalIgnoreCase))
                return true;

            var mmClean = mmValue?.Trim();
            var erxClean = erxValue?.Trim();

            // 0. Autonomous Muse AI Learned Clinical Memory
            // If Muse AI inference or an auditor previously learned/approved this pair as equivalent, evaluate as Match.
            var drugContext = record?.MmDispensedDrugName ?? record?.ErxDrugName;
            if (MuseClinicalMemoryStore.TryGetResolution(rule.FieldGroupName ?? "General", mmClean, erxClean, drugContext, out var learnedEntry) && learnedEntry != null)
            {
                if (learnedEntry.IsCompliant)
                {
                    _logger.LogInformation(
                        "[ComparisonEngine:EvaluateMatchForRecord] Rx {RxNumber}: Discrepancy eliminated via Muse Clinical Memory for rule '{FieldGroupName}': MM '{MmVal}' vs ERX '{ErxVal}'. Reason: {Reason} (Source: {Source}).",
                        record?.RxNumber, rule.FieldGroupName, mmClean, erxClean, learnedEntry.Reason, learnedEntry.Source);
                    return true;
                }
            }

            // Clinical Pharmacy: Special semantic evaluation for Drug Name
            // In Medicine Master, "/" and "\" have been eliminated for DrugName/Brand name.
            // PharmacyDrugNormalizer eliminates slashes, normalizes strengths/dosage forms,
            // and resolves clinical equivalence against Medicine Master.
            // If NDCs match or are equivalent in Medicine Master, the drug product is identical: evaluate as Match.
            if (IsDrugNameRule(rule))
            {
                if (record != null && (
                    AreNdcsMatching(record.MmDispensedNdc, record.ErxNdc) ||
                    PharmacyDrugNormalizer.AreNdcsEquivalent(record.MmDispensedNdc, record.ErxNdc, record, _dbContext)))
                {
                    _logger.LogInformation(
                        "[ComparisonEngine:EvaluateMatchForRecord] Rx {RxNumber}: Drug Name discrepancy skipped because NDCs match or are equivalent in Medicine Master (MM: '{MmNdc}', ERX: '{ErxNdc}').",
                        record.RxNumber, record.MmDispensedNdc, record.ErxNdc);
                    return true;
                }

                if (PharmacyDrugNormalizer.AreDrugsClinicallyEquivalent(
                    mmClean,
                    erxClean,
                    record?.MmPotencyUnitCode,
                    record?.ErxPotencyUnitCode,
                    _dbContext))
                {
                    return true;
                }
            }

            // Clinical Pharmacy: Special NDC evaluation
            // If NDCs match between MicroMerchant and ERX or are equivalent in Medicine Master, evaluate as Match.
            if (IsNdcRule(rule))
            {
                if (AreNdcsMatching(mmClean, erxClean) ||
                    (record != null && AreNdcsMatching(record.MmDispensedNdc, record.ErxNdc)) ||
                    PharmacyDrugNormalizer.AreNdcsEquivalent(mmClean, erxClean, record, _dbContext) ||
                    (record != null && PharmacyDrugNormalizer.AreNdcsEquivalent(record.MmDispensedNdc, record.ErxNdc, record, _dbContext)))
                {
                    _logger.LogInformation(
                        "[ComparisonEngine:EvaluateMatchForRecord] Rx {RxNumber}: NDC discrepancy skipped because NDCs match or are equivalent in Medicine Master (MM: '{MmNdc}', ERX: '{ErxNdc}').",
                        record?.RxNumber, mmClean ?? record?.MmDispensedNdc, erxClean ?? record?.ErxNdc);
                    return true;
                }
            }

            // Clinical Pharmacy: Patient Name & Demographics Cross-Validation
            // 1. Exact string match (case-insensitive).
            // 2. Clinical Name Equivalence (token-based, suffixes like Jr/Sr, common nicknames like Sam <-> Sammy).
            // 3. Demographic cross-validation (matching DOB + phone/address/shared last name).
            if (IsPatientNameRule(rule))
            {
                if (string.Equals(mmClean, erxClean, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (ArePatientNamesClinicallyEquivalent(mmClean, erxClean, record))
                {
                    _logger.LogInformation(
                        "[ComparisonEngine:EvaluateMatchForRecord] Rx {RxNumber}: Patient Name discrepancy eliminated because names are clinically equivalent (MM: '{MmName}', ERX: '{ErxName}').",
                        record?.RxNumber, mmClean, erxClean);
                    return true;
                }

                if (record != null && ArePatientDemographicsMatching(record))
                {
                    _logger.LogInformation(
                        "[ComparisonEngine:EvaluateMatchForRecord] Rx {RxNumber}: Patient Name discrepancy eliminated for MM '{MmName}' vs ERX '{ErxName}' because Patient Demographics (DOB/Phone/Address) confirm identity.",
                        record.RxNumber, mmClean, erxClean);
                    return true;
                }
            }

            // Clinical Pharmacy: DEA Evaluation with Controlled Substance Master & Zero Validation
            // 1. If drug is non-controlled, DEA is not required and not counted as a discrepancy.
            // 2. If drug is a known controlled substance (C-II through C-V), Prescriber DEA is mandatory.
            //    If DEA is missing or contains '0', flag specifically as missing/invalid DEA violation.
            if (IsDeaRule(rule))
            {
                if (record != null)
                {
                    var isControlled = (_referenceDataService?.IsControlledSubstance(record) ?? false) ||
                                       _deaScheduleService.IsClass2Narcotic(record) ||
                                       _deaScheduleService.IsClass3To5Narcotic(record);
                    if (!isControlled)
                    {
                        _logger.LogInformation(
                            "[ComparisonEngine:EvaluateMatchForRecord] Rx {RxNumber}: DEA discrepancy skipped for rule '{FieldGroupName}' because drug '{DrugName}' is non-controlled.",
                            record.RxNumber, rule.FieldGroupName, record.MmDispensedDrugName ?? record.ErxDrugName ?? "Unknown");
                        return true;
                    }

                    // Controlled substance: validate presence and non-zero format
                    var mmDea = record.MmPrescriberDea?.Trim();
                    var erxDea = record.ErxPrescriberDea?.Trim();

                    var mmZeroOrMissing = IsMissingOrZeroDea(mmDea);
                    var erxZeroOrMissing = IsMissingOrZeroDea(erxDea);

                    if (mmZeroOrMissing || erxZeroOrMissing)
                    {
                        customMmVal = mmZeroOrMissing ? "Missing/Invalid DEA ('0')" : mmDea;
                        customErxVal = erxZeroOrMissing ? "Missing/Invalid DEA ('0')" : erxDea;
                        _logger.LogInformation(
                            "[ComparisonEngine:EvaluateMatchForRecord] Rx {RxNumber}: Prescriber DEA missing or '0' for controlled substance '{DrugName}' (MM: '{MmDea}' vs ERX: '{ErxDea}'). Flagging DEA violation.",
                            record.RxNumber, record.MmDispensedDrugName ?? record.ErxDrugName ?? "Controlled Drug", customMmVal, customErxVal);
                        return false;
                    }

                    if (!string.Equals(mmDea, erxDea, StringComparison.OrdinalIgnoreCase))
                    {
                        customMmVal = mmDea;
                        customErxVal = erxDea;
                        return false;
                    }

                    return true;
                }
            }

            // Clinical Pharmacy: Quantity Ordered Evaluation
            // Evaluates whether Quantity Ordered matches between MicroMerchant and ERX,
            // including clinical package size and unit-of-measure equivalence:
            // 1. Package Size vs Units (e.g. 1 bottle vs 60 mL, 1 bottle vs 10 mL Cromolyn, 1 box vs 15 mL Insulin)
            // 2. Manufacturer-defined puffs / actuations (e.g. 200 puffs or 50 puffs vs 1 canister or 18 g)
            if (IsQuantityOrderedRule(rule))
            {
                if (record != null)
                {
                    var isCompliant = IsQuantityOrderedCompliant(record, rule.ToleranceValue ?? 0.00m);
                    if (isCompliant)
                    {
                        _logger.LogInformation(
                            "[ComparisonEngine:EvaluateMatchForRecord] Rx {RxNumber}: Quantity Ordered match via package size / unit / puff equivalence (MM: '{MmVal}', ERX: '{ErxVal}').",
                            record.RxNumber, mmClean, erxClean);
                        return true;
                    }
                    else
                    {
                        customMmVal = mmClean;
                        customErxVal = erxClean;
                        _logger.LogInformation(
                            "[ComparisonEngine:EvaluateMatchForRecord] Rx {RxNumber}: Quantity Ordered mismatch (MM: '{MmVal}' vs ERX: '{ErxVal}').",
                            record.RxNumber, mmClean, erxClean);
                        return false;
                    }
                }
                else if (TryParseNumeric(mmClean ?? "", out var mmQty) && TryParseNumeric(erxClean ?? "", out var erxQty))
                {
                    return Math.Abs(mmQty - erxQty) <= (rule.ToleranceValue ?? 0.00m);
                }
            }

            // Clinical Pharmacy: Quantity Dispensed must be less than or equal to Quantity Ordered
            if (IsQuantityDispensedRule(rule))
            {
                decimal? dispensed = record?.MmQtyDispensed;
                decimal? ordered = record?.ErxQtyOrd.HasValue == true && record.ErxQtyOrd.Value > 0
                    ? record.ErxQtyOrd.Value
                    : record?.MmQtyOrd.HasValue == true && record.MmQtyOrd.Value > 0
                    ? record.MmQtyOrd.Value
                    : record?.ErxQtyDispensed.HasValue == true && record.ErxQtyDispensed.Value > 0
                    ? record.ErxQtyDispensed.Value
                    : (decimal?)null;

                if (record != null && (!ordered.HasValue || ordered.Value <= 0))
                {
                    var sig = !string.IsNullOrWhiteSpace(record.ErxSig) ? record.ErxSig : record.MmDispensedSig;
                    var days = record.ErxDaysSupply.HasValue && record.ErxDaysSupply.Value > 0
                        ? record.ErxDaysSupply.Value
                        : record.MmDispensedDaysSupply;
                    var sigQty = PharmacySigDosingCalculator.CalculateRequiredQuantity(days, sig);
                    if (sigQty.HasValue && sigQty.Value > 0)
                    {
                        ordered = sigQty.Value;
                    }
                }

                if (record == null)
                {
                    if (TryParseNumeric(mmClean ?? "", out var dNum)) dispensed = dNum;
                    if (TryParseNumeric(erxClean ?? "", out var oNum)) ordered = oNum;
                }

                if (!dispensed.HasValue && !ordered.HasValue)
                {
                    return true;
                }

                if (dispensed.HasValue && ordered.HasValue)
                {
                    var tol = rule.ToleranceValue ?? 0.00m;
                    var isCompliant = record != null
                        ? IsQuantityDispensedCompliant(record, tol, _metaAiClient)
                        : dispensed.Value <= (ordered.Value + tol);

                    if (isCompliant)
                    {
                        _logger.LogInformation(
                            "[ComparisonEngine:EvaluateMatchForRecord] Rx {RxNumber}: Quantity Dispensed ({DispVal}) <= Quantity Ordered / SIG Dosing ({OrdVal}), evaluating as compliant match.",
                            record?.RxNumber, dispensed.Value, ordered.Value);
                        return true;
                    }
                    else
                    {
                        customMmVal = dispensed.Value.ToString("0.##", CultureInfo.InvariantCulture);
                        customErxVal = ordered.Value.ToString("0.##", CultureInfo.InvariantCulture);
                        _logger.LogInformation(
                            "[ComparisonEngine:EvaluateMatchForRecord] Rx {RxNumber}: Quantity Dispensed ({DispVal}) > Quantity Ordered ({OrdVal}), flagging discrepancy.",
                            record?.RxNumber, dispensed.Value, ordered.Value);
                        return false;
                    }
                }

                if (!dispensed.HasValue || !ordered.HasValue)
                {
                    return false;
                }
            }

            // Clinical Pharmacy: ERX Based Days Supply Calculation (Erx Qty / Units per Day from SIG)
            if (IsDaysSupplyRule(rule))
            {
                int? mmDays = record?.MmDispensedDaysSupply;
                if (!mmDays.HasValue && TryParseNumeric(mmClean ?? "", out var parsedMmDays))
                    mmDays = (int)parsedMmDays;

                int? erxBasedDays = record != null ? CalculateErxBasedDaysSupply(record) : null;
                if (!erxBasedDays.HasValue && TryParseNumeric(erxClean ?? "", out var parsedErxDays))
                    erxBasedDays = (int)parsedErxDays;

                if (!mmDays.HasValue && !erxBasedDays.HasValue)
                {
                    return true;
                }

                if (mmDays.HasValue && erxBasedDays.HasValue)
                {
                    var isCompliant = record != null
                        ? IsDaysSupplyCompliant(record, rule.ToleranceValue ?? 0.00m, _metaAiClient)
                        : Math.Abs(mmDays.Value - erxBasedDays.Value) <= (int)(rule.ToleranceValue ?? 0);

                    if (isCompliant)
                    {
                        _logger.LogInformation(
                            "[ComparisonEngine:EvaluateMatchForRecord] Rx {RxNumber}: Days Supply compliant (MM Dispensed: {MmDays}, ERX Based: {ErxDays}).",
                            record?.RxNumber, mmDays.Value, erxBasedDays.Value);
                        return true;
                    }
                    else
                    {
                        customMmVal = mmDays.Value.ToString();
                        customErxVal = erxBasedDays.Value.ToString();
                        _logger.LogInformation(
                            "[ComparisonEngine:EvaluateMatchForRecord] Rx {RxNumber}: Days Supply mismatch (MM Dispensed: {MmDays} vs ERX Based: {ErxDays}).",
                            record?.RxNumber, mmDays.Value, erxBasedDays.Value);
                        return false;
                    }
                }

                if (!mmDays.HasValue || !erxBasedDays.HasValue)
                {
                    return false;
                }
            }

            switch (matchType.ToLowerInvariant())
            {
                case "drugsemantic":
                case "drugmatch":
                case "drugclinical":
                    if (record != null && AreNdcsMatching(record.MmDispensedNdc, record.ErxNdc)) return true;
                    if (string.IsNullOrEmpty(mmClean) && string.IsNullOrEmpty(erxClean)) return true;
                    if (string.IsNullOrEmpty(mmClean) || string.IsNullOrEmpty(erxClean)) return false;
                    return PharmacyDrugNormalizer.AreDrugsClinicallyEquivalent(
                        mmClean,
                        erxClean,
                        record?.MmPotencyUnitCode,
                        record?.ErxPotencyUnitCode,
                        _dbContext);

                case "ndcmatch":
                case "ndcsemantic":
                case "ndcclinical":
                    if (AreNdcsMatching(mmClean, erxClean) || (record != null && AreNdcsMatching(record.MmDispensedNdc, record.ErxNdc))) return true;
                    if (string.IsNullOrEmpty(mmClean) && string.IsNullOrEmpty(erxClean)) return true;
                    if (string.IsNullOrEmpty(mmClean) || string.IsNullOrEmpty(erxClean)) return false;
                    return PharmacyDrugNormalizer.AreNdcsEquivalent(mmClean, erxClean, record, _dbContext);

                case "qtyordered":
                case "quantityordered":
                    if (record != null)
                        return IsQuantityOrderedCompliant(record, rule.ToleranceValue ?? 0.00m);
                    if (TryParseNumeric(mmClean ?? "", out var mOrd) && TryParseNumeric(erxClean ?? "", out var eOrd))
                        return Math.Abs(mOrd - eOrd) <= (rule.ToleranceValue ?? 0.00m);
                    return false;

                case "qtydispensedlessthanorequalordered":
                case "quantitydispensed":
                case "qtydispensed":
                    if (record != null)
                        return IsQuantityDispensedCompliant(record, rule.ToleranceValue ?? 0.00m, _metaAiClient);
                    if (TryParseNumeric(mmClean ?? "", out var dVal) && TryParseNumeric(erxClean ?? "", out var oVal))
                        return dVal <= (oVal + (rule.ToleranceValue ?? 0.00m)) ||
                               PharmacyPackageSizeNormalizer.AreQuantitiesPackageEquivalent(dVal, oVal, record, _dbContext, rule.ToleranceValue ?? 0.00m);
                    return false;

                case "datesameorafter":
                case "date_same_or_after":
                    return EvaluateDateSameOrAfter(record, rule, mmClean, erxClean, out customMmVal, out customErxVal);

                case "effectivedategoverned":
                case "effective_date_governed":
                    return EvaluateEffectiveDateGoverned(record, rule, mmClean, erxClean, out customMmVal, out customErxVal);

                case "datewithindays":
                case "date_within_days":
                    return EvaluateDateWithinDays(record, rule, mmClean, erxClean, (int)(rule.ToleranceValue ?? 0), out customMmVal, out customErxVal);

                case "class2zerorefills":
                case "class2_zero_refills":
                    return EvaluateClass2ZeroRefills(record, rule, mmClean, erxClean, out customMmVal, out customErxVal);

                case "class2expiration30days":
                case "class2_expiration_30days":
                    return EvaluateClass2Expiration(record, rule, (int)(rule.ToleranceValue > 0 ? rule.ToleranceValue.Value : 30), "DEA Rule", out customMmVal, out customErxVal);

                case "class3to5expiration180days":
                case "class3to5_expiration_180days":
                    return EvaluateClass3To5Expiration(record, rule, (int)(rule.ToleranceValue > 0 ? rule.ToleranceValue.Value : 180), "DEA Rule", out customMmVal, out customErxVal);

                case "texasclass2expiration21days":
                case "texas_class2_expiration_21days":
                case "texasclass2expiration30days":
                case "texas_class2_expiration_30days":
                    return EvaluateTexasClass2Expiration(record, rule, out customMmVal, out customErxVal);

                case "staterxvalidity":
                case "state_rx_validity":
                case "prescriptionvaliditybystate":
                case "staterxvaliditygoverned":
                    return EvaluateStateRxValidity(record, rule, out customMmVal, out customErxVal);

                case "supervisingphysicianrequired":
                case "supervisingphysician":
                case "supervising_physician":
                case "panpsupervisingphysician":
                    return EvaluateSupervisingPhysicianRequired(record, rule, out customMmVal, out customErxVal);

                case "exact":
                    if (string.IsNullOrEmpty(mmClean) && string.IsNullOrEmpty(erxClean)) return true;
                    if (string.IsNullOrEmpty(mmClean) || string.IsNullOrEmpty(erxClean)) return false;
                    var exactMmDate = TryParseDate(mmClean);
                    var exactErxDate = TryParseDate(erxClean);
                    if (exactMmDate.HasValue && exactErxDate.HasValue)
                    {
                        var isDateMatch = exactMmDate.Value.Date == exactErxDate.Value.Date;
                        if (!isDateMatch)
                        {
                            customMmVal = exactMmDate.Value.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                            customErxVal = exactErxDate.Value.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                        }
                        return isDateMatch;
                    }
                    return string.Equals(mmClean, erxClean, StringComparison.Ordinal);

                case "sigsemantic":
                case "signormalized":
                case "sigmatch":
                    if (string.IsNullOrEmpty(mmClean) && string.IsNullOrEmpty(erxClean)) return true;
                    if (string.IsNullOrEmpty(mmClean) || string.IsNullOrEmpty(erxClean)) return false;
                    if (string.Equals(mmClean, erxClean, StringComparison.OrdinalIgnoreCase)) return true;
                    if (PharmacySigNormalizer.AreSigsEquivalent(mmClean, erxClean)) return true;

                    var sigPii = ExtractPiiTokens(record);

                    // 1. Try Meta AI if configured
                    if (_metaAiClient != null && _metaAiClient.IsConfigured &&
                        _metaAiClient.AreSigsEquivalent(mmClean, erxClean, sigPii))
                    {
                        return true;
                    }

                    // 2. Try TypeSafe AI
                    if (_typeSafeClient != null &&
                        _typeSafeClient.AreSigsEquivalent(mmClean, erxClean, sigPii))
                    {
                        return true;
                    }

                    return false;

                case "caseinsensitive":
                    if (string.IsNullOrEmpty(mmClean) && string.IsNullOrEmpty(erxClean)) return true;
                    if (string.IsNullOrEmpty(mmClean) || string.IsNullOrEmpty(erxClean)) return false;
                    var caseMmDate = TryParseDate(mmClean);
                    var caseErxDate = TryParseDate(erxClean);
                    if (caseMmDate.HasValue && caseErxDate.HasValue)
                    {
                        var isDateMatch = caseMmDate.Value.Date == caseErxDate.Value.Date;
                        if (!isDateMatch)
                        {
                            customMmVal = caseMmDate.Value.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                            customErxVal = caseErxDate.Value.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                        }
                        return isDateMatch;
                    }

                    // Clinical Pharmacy: Special semantic evaluation for Sig / Directions
                    if (IsSigRule(rule))
                    {
                        if (string.Equals(mmClean, erxClean, StringComparison.OrdinalIgnoreCase)) return true;
                        if (PharmacySigNormalizer.AreSigsEquivalent(mmClean, erxClean)) return true;

                        var caseSigPii = ExtractPiiTokens(record);
                        if (_metaAiClient != null && _metaAiClient.IsConfigured &&
                            _metaAiClient.AreSigsEquivalent(mmClean, erxClean, caseSigPii))
                        {
                            return true;
                        }

                        if (_typeSafeClient != null &&
                            _typeSafeClient.AreSigsEquivalent(mmClean, erxClean, caseSigPii))
                        {
                            return true;
                        }

                        return false;
                    }

                    return string.Equals(mmClean, erxClean, StringComparison.OrdinalIgnoreCase);

                case "numerictolerance":
                    if (string.IsNullOrEmpty(mmClean) && string.IsNullOrEmpty(erxClean)) return true;
                    if (string.IsNullOrEmpty(mmClean) || string.IsNullOrEmpty(erxClean)) return false;
                    if (record != null && IsQuantityOrderedRule(rule))
                        return IsQuantityOrderedCompliant(record, rule.ToleranceValue ?? 0.00m);
                    if (record != null && IsQuantityDispensedRule(rule))
                        return IsQuantityDispensedCompliant(record, rule.ToleranceValue ?? 0.00m, _metaAiClient);
                    return EvaluateNumericTolerance(mmClean, erxClean, rule.ToleranceValue ?? 0.00m);

                case "fuzzy":
                    if (string.IsNullOrEmpty(mmClean) && string.IsNullOrEmpty(erxClean)) return true;
                    if (string.IsNullOrEmpty(mmClean) || string.IsNullOrEmpty(erxClean)) return false;
                    return EvaluateFuzzy(mmClean, erxClean);

                default:
                    if (string.IsNullOrEmpty(mmClean) && string.IsNullOrEmpty(erxClean)) return true;
                    if (string.IsNullOrEmpty(mmClean) || string.IsNullOrEmpty(erxClean)) return false;
                    return string.Equals(mmClean, erxClean, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string?[] ExtractPiiTokens(ErxAuditRecord? record)
        {
            if (record == null) return Array.Empty<string?>();
            return new string?[]
            {
                record.MmPatientName,
                record.ErxPatientName,
                record.MmPatientDob?.ToString("MM/dd/yyyy"),
                record.ErxPatientDob?.ToString("MM/dd/yyyy"),
                record.MmPatientPhone,
                record.ErxPatientPhone,
                record.MmPatientAddress,
                record.ErxPatientAddress,
                record.RxNumber,
                record.ErxRxNumber,
                record.MmPrescriberName,
                record.ErxPrescriberName,
                record.MmPrescriberNpi,
                record.ErxPrescriberNpi,
                record.MmPrescriberDea,
                record.ErxPrescriberDea,
                record.SupervisorName
            };
        }

        private static bool EvaluateDateSameOrAfter(
            ErxAuditRecord? record,
            ErxColumnMatchRule rule,
            string? mmClean,
            string? erxClean,
            out string? customMmVal,
            out string? customErxVal)
        {
            customMmVal = null;
            customErxVal = null;

            var mmDate = TryParseDate(mmClean);
            var erxDate = TryParseDate(erxClean);

            if (record != null)
            {
                if (rule.MmColumnName.Equals("MmDateFilled", StringComparison.OrdinalIgnoreCase))
                    mmDate ??= record.MmDateFilled;
                else if (rule.MmColumnName.Equals("PickupDate", StringComparison.OrdinalIgnoreCase))
                    mmDate ??= record.PickupDate;

                if (rule.ErxColumnName.Equals("ErxOrderDate", StringComparison.OrdinalIgnoreCase))
                    erxDate ??= record.ErxOrderDate;
                else if (rule.ErxColumnName.Equals("MmDateFilled", StringComparison.OrdinalIgnoreCase))
                    erxDate ??= record.MmDateFilled;
                else if (rule.ErxColumnName.Equals("ErxEffectiveDate", StringComparison.OrdinalIgnoreCase))
                    erxDate ??= record.ErxEffectiveDate;
            }

            if (!mmDate.HasValue || !erxDate.HasValue)
                return true;

            if (mmDate.Value.Date < erxDate.Value.Date)
            {
                customMmVal = $"{mmDate.Value:MM/dd/yyyy}";
                customErxVal = $"{erxDate.Value:MM/dd/yyyy} (Must be on or after)";
                return false;
            }

            return true;
        }

        private static bool EvaluateEffectiveDateGoverned(
            ErxAuditRecord? record,
            ErxColumnMatchRule rule,
            string? mmClean,
            string? erxClean,
            out string? customMmVal,
            out string? customErxVal)
        {
            customMmVal = null;
            customErxVal = null;

            var fillDate = TryParseDate(mmClean) ?? record?.MmDateFilled;
            var effDate = TryParseDate(erxClean) ?? record?.ErxEffectiveDate;
            var orderDate = record?.ErxOrderDate;

            if (!fillDate.HasValue) return true;

            // If effective date exists, fill date must be on or after effective date
            if (effDate.HasValue)
            {
                if (fillDate.Value.Date < effDate.Value.Date)
                {
                    customMmVal = $"{fillDate.Value:MM/dd/yyyy} (Date Filled)";
                    customErxVal = $"{effDate.Value:MM/dd/yyyy} (ERX Effective Date)";
                    return false;
                }
            }

            // Also must be on or after order date
            if (orderDate.HasValue && fillDate.Value.Date < orderDate.Value.Date)
            {
                customMmVal = $"{fillDate.Value:MM/dd/yyyy} (Date Filled)";
                customErxVal = $"{orderDate.Value:MM/dd/yyyy} (ERX Order Date)";
                return false;
            }

            return true;
        }

        private static bool EvaluateDateWithinDays(
            ErxAuditRecord? record,
            ErxColumnMatchRule rule,
            string? mmClean,
            string? erxClean,
            int maxDays,
            out string? customMmVal,
            out string? customErxVal)
        {
            customMmVal = null;
            customErxVal = null;

            var date1 = TryParseDate(mmClean);
            var date2 = TryParseDate(erxClean);

            if (!date1.HasValue || !date2.HasValue) return true;

            var daysElapsed = Math.Abs((date1.Value.Date - date2.Value.Date).Days);
            if (daysElapsed > maxDays)
            {
                customMmVal = $"{date1.Value:MM/dd/yyyy} ({daysElapsed} days)";
                customErxVal = $"{date2.Value:MM/dd/yyyy} (Max {maxDays} days)";
                return false;
            }

            return true;
        }

        private bool EvaluateClass2ZeroRefills(
            ErxAuditRecord? record,
            ErxColumnMatchRule rule,
            string? mmClean,
            string? erxClean,
            out string? customMmVal,
            out string? customErxVal)
        {
            customMmVal = null;
            customErxVal = null;

            var isClass2 = _deaScheduleService.IsClass2Narcotic(record);
            if (!isClass2)
            {
                return true; // Not a Class 2 drug, refills are allowed
            }

            var hasErxRefills = TryParseNumeric(erxClean ?? record?.ErxRefill?.ToString() ?? "", out var erxRefillVal) && erxRefillVal > 0;
            var hasMmFill = TryParseNumeric(mmClean ?? record?.MmFillNumber ?? "", out var mmFillVal) && mmFillVal > 0;
            var hasMmPharmacistRefills = record?.MmRefillsPharmacistEntered.HasValue == true && record.MmRefillsPharmacistEntered.Value > 0;

            if (hasErxRefills || hasMmFill || hasMmPharmacistRefills)
            {
                customMmVal = $"{record?.MmFillNumber ?? mmClean ?? "0"} (C-II Refill/Fill)";
                customErxVal = "0 (Class 2 Narcotics prohibited from having refills)";
                return false;
            }

            return true;
        }

        private bool EvaluateClass2Expiration(
            ErxAuditRecord? record,
            ErxColumnMatchRule rule,
            int maxDays,
            string rulePrefix,
            out string? customMmVal,
            out string? customErxVal)
        {
            customMmVal = null;
            customErxVal = null;

            var isClass2 = _deaScheduleService.IsClass2Narcotic(record);
            if (!isClass2)
            {
                return true; // Only applies to Class 2
            }

            var fillDate = record?.MmDateFilled;
            var governingDate = record?.ErxEffectiveDate ?? record?.ErxOrderDate;

            if (!fillDate.HasValue || !governingDate.HasValue)
                return true;

            var daysElapsed = (fillDate.Value.Date - governingDate.Value.Date).Days;
            if (daysElapsed > maxDays)
            {
                customMmVal = $"{fillDate.Value:MM/dd/yyyy} ({daysElapsed} days elapsed)";
                customErxVal = $"{governingDate.Value:MM/dd/yyyy} (Max {maxDays} days allowed - {rulePrefix})";
                return false;
            }

            return true;
        }

        private bool EvaluateClass3To5Expiration(
            ErxAuditRecord? record,
            ErxColumnMatchRule rule,
            int maxDays,
            string rulePrefix,
            out string? customMmVal,
            out string? customErxVal)
        {
            customMmVal = null;
            customErxVal = null;

            var isClass3To5 = _deaScheduleService.IsClass3To5Narcotic(record);
            if (!isClass3To5)
            {
                return true; // Only applies to Class 3-5
            }

            var fillDate = record?.MmDateFilled;
            var governingDate = record?.ErxEffectiveDate ?? record?.ErxOrderDate;

            if (!fillDate.HasValue || !governingDate.HasValue)
                return true;

            var daysElapsed = (fillDate.Value.Date - governingDate.Value.Date).Days;
            if (daysElapsed > maxDays)
            {
                customMmVal = $"{fillDate.Value:MM/dd/yyyy} ({daysElapsed} days elapsed)";
                customErxVal = $"{governingDate.Value:MM/dd/yyyy} (Max {maxDays} days allowed - {rulePrefix})";
                return false;
            }

            return true;
        }

        private bool EvaluateTexasClass2Expiration(
            ErxAuditRecord? record,
            ErxColumnMatchRule rule,
            out string? customMmVal,
            out string? customErxVal)
        {
            customMmVal = null;
            customErxVal = null;

            var isTexas = IsTexasRecord(record);
            if (!isTexas)
            {
                return true; // Not a Texas prescription, Texas rule does not apply
            }

            // Client discussion update: Texas has changed validity for Class 2 from 21 days to 30 days
            var maxDays = (int)(rule.ToleranceValue > 0 ? rule.ToleranceValue.Value : 30);
            return EvaluateClass2Expiration(record, rule, maxDays, $"Texas {maxDays}-Day Rule", out customMmVal, out customErxVal);
        }

        private bool EvaluateStateRxValidity(
            ErxAuditRecord? record,
            ErxColumnMatchRule rule,
            out string? customMmVal,
            out string? customErxVal)
        {
            customMmVal = null;
            customErxVal = null;

            if (record == null) return true;

            var fillDate = record.MmDateFilled;
            var governingDate = record.ErxEffectiveDate ?? record.ErxOrderDate ?? record.MmOrderDate;

            if (!fillDate.HasValue || !governingDate.HasValue)
                return true;

            // Resolve State
            var resolvedState = _referenceDataService?.ResolveStateFromAddress(record.MmPatientAddress, record.ErxPatientAddress);
            if (string.IsNullOrEmpty(resolvedState))
            {
                resolvedState = IsTexasRecord(record) ? "TX" : "US";
            }

            var stateRule = _referenceDataService?.GetStateValidity(resolvedState);

            // Determine Schedule
            var schedule = _referenceDataService?.GetDeaSchedule(record)
                           ?? _deaScheduleService.GetDeaSchedule(record);

            int maxDays;
            string scheduleLabel;

            if (schedule == 2)
            {
                maxDays = stateRule?.Schedule2ValidityDays ?? (int)(rule.ToleranceValue > 0 ? rule.ToleranceValue.Value : 30);
                scheduleLabel = "Schedule II";
            }
            else if (schedule >= 3 && schedule <= 5)
            {
                maxDays = stateRule?.Schedule3To5ValidityDays ?? (int)(rule.ToleranceValue > 0 ? rule.ToleranceValue.Value : 180);
                scheduleLabel = $"Schedule {schedule}";
            }
            else
            {
                maxDays = stateRule?.NonControlledValidityDays ?? (int)(rule.ToleranceValue > 0 ? rule.ToleranceValue.Value : 365);
                scheduleLabel = "Non-Controlled";
            }

            var daysElapsed = (fillDate.Value.Date - governingDate.Value.Date).Days;
            if (daysElapsed > maxDays)
            {
                customMmVal = $"{fillDate.Value:MM/dd/yyyy} ({daysElapsed} days elapsed)";
                customErxVal = $"{governingDate.Value:MM/dd/yyyy} (Max {maxDays} days valid under {stateRule?.StateName ?? resolvedState} law for {scheduleLabel})";
                _logger.LogInformation(
                    "[ComparisonEngine:EvaluateStateRxValidity] Rx {RxNumber}: Prescription expired under {State} law for {Sched} ({Elapsed} days elapsed vs max {MaxDays} days).",
                    record.RxNumber, stateRule?.StateName ?? resolvedState, scheduleLabel, daysElapsed, maxDays);
                return false;
            }

            return true;
        }

        private bool EvaluateSupervisingPhysicianRequired(
            ErxAuditRecord? record,
            ErxColumnMatchRule rule,
            out string? customMmVal,
            out string? customErxVal)
        {
            customMmVal = null;
            customErxVal = null;

            if (record == null) return true;

            var prescriberNpi = record.ErxPrescriberNpi ?? record.MmPrescriberNpi;
            var address = string.Concat(record.MmPatientAddress, " ", record.ErxPatientAddress);

            var isRequired = false;
            string providerType = "PA/NP";
            string resolvedState = "US";

            if (_referenceDataService != null)
            {
                isRequired = _referenceDataService.IsSupervisingPhysicianRequired(
                    prescriberNpi,
                    address,
                    out providerType,
                    out resolvedState);
            }

            // Fallback heuristic: check if prescriber name has PA or NP title and check state rule
            if (!isRequired && (string.IsNullOrEmpty(providerType) || providerType == "PA/NP"))
            {
                var presName = string.Concat(record.ErxPrescriberName, " ", record.MmPrescriberName).ToUpperInvariant();
                if (Regex.IsMatch(presName, @"\b(PA-C|PA|PHYSICIAN ASSISTANT)\b"))
                {
                    providerType = "PA";
                    resolvedState = _referenceDataService?.ResolveStateFromAddress(record.MmPatientAddress, record.ErxPatientAddress) ?? (IsTexasRecord(record) ? "TX" : "US");
                    var supRule = _referenceDataService?.GetSupervisingRule(resolvedState);
                    isRequired = supRule?.PaSupervisingRequired ?? false;
                }
                else if (Regex.IsMatch(presName, @"\b(NP|FNP|ARNP|APRN|CRNP|NURSE PRACTITIONER)\b"))
                {
                    providerType = "NP";
                    resolvedState = _referenceDataService?.ResolveStateFromAddress(record.MmPatientAddress, record.ErxPatientAddress) ?? (IsTexasRecord(record) ? "TX" : "US");
                    var supRule = _referenceDataService?.GetSupervisingRule(resolvedState);
                    isRequired = supRule?.NpSupervisingRequired ?? false;
                }
            }

            if (!isRequired)
            {
                return true; // Not required by state law or provider is not PA/NP
            }

            // Supervising physician IS required!
            var supervisor = record.SupervisorName?.Trim();
            if (string.IsNullOrWhiteSpace(supervisor))
            {
                customMmVal = "Missing Supervising Physician";
                customErxVal = $"Mandated for {providerType} in {resolvedState}";
                _logger.LogInformation(
                    "[ComparisonEngine:EvaluateSupervisingPhysicianRequired] Rx {RxNumber}: Missing supervising physician for {ProviderType} in {State} (NPI: {Npi}). Flagging discrepancy.",
                    record.RxNumber, providerType, resolvedState, prescriberNpi);
                return false;
            }

            return true;
        }

        private static bool IsMissingOrZeroDea(string? dea)
        {
            if (string.IsNullOrWhiteSpace(dea)) return true;
            var trimmed = dea.Trim();
            if (trimmed == "0") return true;
            if (trimmed.All(c => c == '0' || c == '-')) return true;
            var alphaNum = Regex.Replace(trimmed, @"[^\w]", "");
            if (string.IsNullOrEmpty(alphaNum) || alphaNum.All(c => c == '0')) return true;
            return false;
        }

        private static bool IsTexasRecord(ErxAuditRecord? record)
        {
            if (record == null) return false;
            var addresses = string.Concat(record.MmPatientAddress, " ", record.ErxPatientAddress);
            if (string.IsNullOrWhiteSpace(addresses)) return false;

            return Regex.IsMatch(addresses, @"\b(TX|Texas)\b", RegexOptions.IgnoreCase);
        }

        private static bool EvaluateNumericTolerance(string mmClean, string erxClean, decimal tolerance)
        {
            var parsedMm = TryParseNumeric(mmClean, out var mmNum);
            var parsedErx = TryParseNumeric(erxClean, out var erxNum);

            if (parsedMm && parsedErx)
            {
                return Math.Abs(mmNum - erxNum) <= tolerance;
            }

            return string.Equals(mmClean, erxClean, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseNumeric(string input, out decimal result)
        {
            var cleaned = Regex.Replace(input, @"[^\d.-]", "");
            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        private static DateTime? TryParseDate(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            if (DateTime.TryParseExact(input, UsaDateFormats, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out var dt))
                return dt;

            if (DateTime.TryParse(input, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out dt))
                return dt;

            if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt;

            // Handle Excel serial date (e.g. 45694 -> 2025-06-02)
            if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial) && serial > 30000 && serial < 80000)
            {
                try
                {
                    return DateTime.FromOADate(serial);
                }
                catch { }
            }

            return null;
        }

        private static bool EvaluateFuzzy(string s1, string s2)
        {
            if (string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase))
                return true;

            var norm1 = NormalizeForFuzzy(s1);
            var norm2 = NormalizeForFuzzy(s2);

            if (norm1 == norm2) return true;
            if (norm1.Contains(norm2, StringComparison.OrdinalIgnoreCase) ||
                norm2.Contains(norm1, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var tokens1 = norm1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var tokens2 = norm2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens1.Length > 0 && tokens2.Length > 0)
            {
                var common = tokens1.Intersect(tokens2, StringComparer.OrdinalIgnoreCase).Count();
                var maxLen = Math.Max(tokens1.Length, tokens2.Length);
                if (maxLen > 0 && (double)common / maxLen >= 0.70)
                    return true;
            }

            var similarity = ComputeLevenshteinSimilarity(norm1, norm2);
            return similarity >= 0.80;
        }

        private static string NormalizeForFuzzy(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var withoutPunctuation = Regex.Replace(text, @"[^\w\s]", " ");
            return Regex.Replace(withoutPunctuation, @"\s+", " ").Trim().ToLowerInvariant();
        }

        private static double ComputeLevenshteinSimilarity(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 1.0 : 0.0;
            if (string.IsNullOrEmpty(t)) return 0.0;

            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            for (var i = 0; i <= n; i++) d[i, 0] = i;
            for (var j = 0; j <= m; j++) d[0, j] = j;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            var distance = d[n, m];
            var maxLength = Math.Max(n, m);
            return 1.0 - ((double)distance / maxLength);
        }

        public static string? GetPropertyValueAsString(ErxAuditRecord record, string? propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return null;

            if (propertyName.Equals("Refills Originally prescriber sent", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Replace(" ", "").Equals("RefillsOriginallyprescribersent", StringComparison.OrdinalIgnoreCase))
            {
                propertyName = nameof(ErxAuditRecord.ErxRefill);
            }

            var prop = PropertyCache.GetOrAdd(propertyName, name =>
                typeof(ErxAuditRecord).GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

            if (prop == null) return null;

            var val = prop.GetValue(record);
            if (val == null) return null;

            if (val is DateTime dt)
            {
                return dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)
                    : dt.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            }

            if (val is TimeSpan ts)
            {
                return ts.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
            }

            if (val is decimal dec)
            {
                return dec.ToString("G29", CultureInfo.InvariantCulture);
            }

            return val.ToString();
        }

        private static bool IsSigRule(ErxColumnMatchRule rule)
        {
            if (rule == null) return false;
            return string.Equals(rule.FieldGroupName, "Sig / Directions", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rule.FieldGroupName, "Sig", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rule.MmColumnName, "MmDispensedSig", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rule.ErxColumnName, "ErxSig", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDrugNameRule(ErxColumnMatchRule rule)
        {
            if (rule == null) return false;
            var fg = rule.FieldGroupName?.Trim() ?? string.Empty;
            var mm = rule.MmColumnName?.Trim() ?? string.Empty;
            var erx = rule.ErxColumnName?.Trim() ?? string.Empty;
            return string.Equals(fg, "Drug Name", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(fg, "Drug", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(fg, "Drug Name / Description", StringComparison.OrdinalIgnoreCase) ||
                   fg.IndexOf("Drug Name", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   string.Equals(fg, "Drug & Product Details", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mm, "MmDispensedDrugName", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(erx, "ErxDrugName", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsNdcRule(ErxColumnMatchRule rule)
        {
            if (rule == null) return false;
            var fg = rule.FieldGroupName?.Trim() ?? string.Empty;
            var mm = rule.MmColumnName?.Trim() ?? string.Empty;
            var erx = rule.ErxColumnName?.Trim() ?? string.Empty;
            return string.Equals(fg, "NDC", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(fg, "NDC Code", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(fg, "NDC Number", StringComparison.OrdinalIgnoreCase) ||
                   fg.IndexOf("NDC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   string.Equals(mm, "MmDispensedNdc", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(erx, "ErxNdc", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsPatientNameRule(ErxColumnMatchRule rule)
        {
            if (rule == null) return false;
            return string.Equals(rule.FieldGroupName, "Patient Name", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rule.FieldGroupName, "Patient", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rule.MmColumnName, "MmPatientName", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rule.ErxColumnName, "ErxPatientName", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDeaRule(ErxColumnMatchRule rule)
        {
            if (rule == null) return false;

            var fg = rule.FieldGroupName?.Trim() ?? string.Empty;
            var mm = rule.MmColumnName?.Trim() ?? string.Empty;
            var erx = rule.ErxColumnName?.Trim() ?? string.Empty;
            var matchType = rule.MatchType?.Trim() ?? string.Empty;

            // Do not treat Expiration or Refill compliance rules as DEA identity rules
            if (matchType.StartsWith("Class2", StringComparison.OrdinalIgnoreCase) ||
                matchType.StartsWith("Class3To5", StringComparison.OrdinalIgnoreCase) ||
                matchType.StartsWith("Texas", StringComparison.OrdinalIgnoreCase) ||
                matchType.StartsWith("State", StringComparison.OrdinalIgnoreCase) ||
                matchType.IndexOf("Expiration", StringComparison.OrdinalIgnoreCase) >= 0 ||
                matchType.IndexOf("Validity", StringComparison.OrdinalIgnoreCase) >= 0 ||
                matchType.IndexOf("Refill", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fg.IndexOf("Expiration", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fg.IndexOf("Validity", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fg.IndexOf("Refill", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (string.Equals(mm, "MmPrescriberDea", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(erx, "ErxPrescriberDea", StringComparison.OrdinalIgnoreCase) ||
                mm.IndexOf("PrescriberDea", StringComparison.OrdinalIgnoreCase) >= 0 ||
                erx.IndexOf("PrescriberDea", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (fg.IndexOf("Prescriber DEA", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fg.IndexOf("Doctor DEA", StringComparison.OrdinalIgnoreCase) >= 0 ||
                string.Equals(fg, "DEA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fg, "DEA Number", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fg, "Prescriber's DEA", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static bool IsQuantityOrderedRule(ErxColumnMatchRule rule)
        {
            if (rule == null) return false;
            if (rule.RuleId == 4) return true;

            var fg = rule.FieldGroupName?.Trim() ?? string.Empty;
            var mm = rule.MmColumnName?.Trim() ?? string.Empty;
            var erx = rule.ErxColumnName?.Trim() ?? string.Empty;
            var matchType = rule.MatchType?.Trim() ?? string.Empty;

            if (string.Equals(fg, "Quantity Ordered", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fg, "Qty Ordered", StringComparison.OrdinalIgnoreCase) ||
                fg.IndexOf("Quantity Ordered", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fg.IndexOf("Qty Ordered", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (string.Equals(mm, "MmQtyOrd", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(erx, "ErxQtyOrd", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (matchType.IndexOf("QtyOrd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                matchType.IndexOf("QuantityOrdered", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        public static bool IsQuantityOrderedCompliant(ErxAuditRecord? record, decimal tolerance = 0.00m)
        {
            if (record == null) return false;

            decimal? mmOrd = record.MmQtyOrd.HasValue && record.MmQtyOrd.Value > 0
                ? record.MmQtyOrd.Value
                : record.MmQtyDispensed.HasValue && record.MmQtyDispensed.Value > 0
                ? record.MmQtyDispensed.Value
                : (decimal?)null;

            decimal? erxOrd = record.ErxQtyOrd.HasValue && record.ErxQtyOrd.Value > 0
                ? record.ErxQtyOrd.Value
                : record.ErxQtyDispensed.HasValue && record.ErxQtyDispensed.Value > 0
                ? record.ErxQtyDispensed.Value
                : (decimal?)null;

            if (!mmOrd.HasValue && !erxOrd.HasValue) return true;
            if (!mmOrd.HasValue || !erxOrd.HasValue) return false;

            var tol = tolerance > 0 ? tolerance : 0.00m;

            // 1. Direct equality within tolerance
            if (Math.Abs(mmOrd.Value - erxOrd.Value) <= tol)
                return true;

            // 2. Clinical Package Size and Puff Equivalence:
            // e.g. 1 bottle vs 60 mL, 1 bottle vs 10 mL (Cromolyn), 1 canister vs 200 puffs or 50 puffs, 1 box vs 15 mL insulin
            if (PharmacyPackageSizeNormalizer.AreQuantitiesPackageEquivalent(mmOrd.Value, erxOrd.Value, record, null, tol))
                return true;

            // 3. Fallback: Check if MM Qty Dispensed is package-equivalent to ERX Qty Ordered
            if (record.MmQtyDispensed.HasValue && record.MmQtyDispensed.Value > 0 &&
                PharmacyPackageSizeNormalizer.AreQuantitiesPackageEquivalent(record.MmQtyDispensed.Value, erxOrd.Value, record, null, tol))
            {
                return true;
            }

            return false;
        }

        public static bool IsQuantityDispensedRule(ErxColumnMatchRule rule)
        {
            if (rule == null) return false;
            if (rule.RuleId == 5) return true;

            var fg = rule.FieldGroupName?.Trim() ?? string.Empty;
            var mm = rule.MmColumnName?.Trim() ?? string.Empty;
            var erx = rule.ErxColumnName?.Trim() ?? string.Empty;
            var matchType = rule.MatchType?.Trim() ?? string.Empty;

            if (string.Equals(fg, "Quantity Dispensed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fg, "Qty Dispensed", StringComparison.OrdinalIgnoreCase) ||
                fg.IndexOf("Quantity Dispensed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fg.IndexOf("Qty Dispensed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (string.Equals(mm, "MmQtyDispensed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (matchType.IndexOf("QtyDispensed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                matchType.IndexOf("QuantityDispensed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        public static bool IsQuantityDispensedCompliant(ErxAuditRecord? record, decimal tolerance = 0.00m, IMetaAiClient? metaAiClient = null)
        {
            if (record == null) return false;

            decimal? dispensed = record.MmQtyDispensed;
            decimal? ordered = record.ErxQtyOrd.HasValue && record.ErxQtyOrd.Value > 0
                ? record.ErxQtyOrd.Value
                : record.MmQtyOrd.HasValue && record.MmQtyOrd.Value > 0
                ? record.MmQtyOrd.Value
                : record.ErxQtyDispensed.HasValue && record.ErxQtyDispensed.Value > 0
                ? record.ErxQtyDispensed.Value
                : (decimal?)null;

            var sig = !string.IsNullOrWhiteSpace(record.ErxSig) ? record.ErxSig : record.MmDispensedSig;
            var days = record.ErxDaysSupply.HasValue && record.ErxDaysSupply.Value > 0
                ? record.ErxDaysSupply.Value
                : record.MmDispensedDaysSupply;

            var tol = tolerance > 0 ? tolerance : 0.00m;

            // If ordered quantity is missing or 0, calculate required quantity from Days Supply and SIG
            if ((!ordered.HasValue || ordered.Value <= 0) && days.HasValue && !string.IsNullOrWhiteSpace(sig))
            {
                var sigCalculatedQty = PharmacySigDosingCalculator.CalculateRequiredQuantity(days, sig);
                if (sigCalculatedQty.HasValue && sigCalculatedQty.Value > 0)
                {
                    ordered = sigCalculatedQty.Value;
                }
            }

            if (!dispensed.HasValue && !ordered.HasValue) return true;
            if (!dispensed.HasValue || !ordered.HasValue) return false;

            // 1. Clinical Package Size / Puff / Unit Equivalence check:
            // e.g. Doctor ordered 1 bottle, pharmacist dispensed 10 mL (Cromolyn) or 60 mL
            // Doctor ordered 1 inhaler, pharmacist dispensed 200 puffs or 18 g
            // Doctor ordered 200 puffs, pharmacist dispensed 1 inhaler
            if (PharmacyPackageSizeNormalizer.AreQuantitiesPackageEquivalent(dispensed.Value, ordered.Value, record, null, tol))
                return true;

            // 2. Normalize ordered to dispensed units (e.g. 1 bottle -> 60 mL or 10 mL)
            var normalizedOrdered = PharmacyPackageSizeNormalizer.NormalizeOrderedToDispensedUnits(ordered.Value, dispensed.Value, record, null);

            // 3. Standard check: Dispensed <= Normalized Ordered + tolerance
            if (dispensed.Value <= (normalizedOrdered + tol))
                return true;

            // 4. Check if dispensed matches the required quantity calculated directly from SIG and Days Supply
            if (days.HasValue && !string.IsNullOrWhiteSpace(sig))
            {
                var sigCalculatedQty = PharmacySigDosingCalculator.CalculateRequiredQuantity(days, sig);
                if (sigCalculatedQty.HasValue && Math.Abs(dispensed.Value - sigCalculatedQty.Value) <= tol)
                    return true;
            }

            // 5. Meta AI Clinical Evaluation fallback for Quantity Dispensed
            if (metaAiClient != null && metaAiClient.IsConfigured && dispensed.HasValue)
            {
                var targetOrdered = ordered ?? dispensed.Value;
                var drug = record.MmDispensedDrugName ?? record.ErxDrugName;
                var pii = ExtractPiiTokens(record);

                if (metaAiClient.IsQuantityDispensedCompliant(dispensed.Value, targetOrdered, sig, drug, days, pii))
                {
                    return true;
                }
            }

            return false;
        }

        bool IErxComparisonEngine.IsQuantityDispensedCompliant(ErxAuditRecord? record, decimal tolerance)
        {
            return IsQuantityDispensedCompliant(record, tolerance, _metaAiClient);
        }

        bool IErxComparisonEngine.IsQuantityOrderedCompliant(ErxAuditRecord? record, decimal tolerance)
        {
            return IsQuantityOrderedCompliant(record, tolerance);
        }

        bool IErxComparisonEngine.ArePatientNamesClinicallyEquivalent(string? mmName, string? erxName, ErxAuditRecord? record)
        {
            return ArePatientNamesClinicallyEquivalent(mmName, erxName, record);
        }

        bool IErxComparisonEngine.ArePatientDemographicsMatching(ErxAuditRecord? record)
        {
            return ArePatientDemographicsMatching(record);
        }

        bool IErxComparisonEngine.AreNdcsMatching(string? mmNdc, string? erxNdc)
        {
            return AreNdcsMatching(mmNdc, erxNdc);
        }

        bool IErxComparisonEngine.AreNdcsMatching(ErxAuditRecord? record)
        {
            if (record == null) return false;
            if (AreNdcsMatching(record.MmDispensedNdc, record.ErxNdc)) return true;
            if (_dbContext != null && PharmacyDrugNormalizer.AreNdcsEquivalent(record.MmDispensedNdc, record.ErxNdc, record, _dbContext))
                return true;
            return false;
        }

        public static bool IsDaysSupplyRule(ErxColumnMatchRule rule)
        {
            if (rule == null) return false;
            if (rule.RuleId == 6) return true;

            var fg = rule.FieldGroupName?.Trim() ?? string.Empty;
            var mm = rule.MmColumnName?.Trim() ?? string.Empty;
            var erx = rule.ErxColumnName?.Trim() ?? string.Empty;

            if (string.Equals(fg, "Days Supply", StringComparison.OrdinalIgnoreCase) ||
                fg.IndexOf("Days Supply", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fg.IndexOf("Day Supply", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (string.Equals(mm, "MmDispensedDaysSupply", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(erx, "ErxDaysSupply", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static int? CalculateErxBasedDaysSupply(ErxAuditRecord? record)
        {
            if (record == null) return null;

            decimal? erxQty = record.ErxQtyOrd.HasValue && record.ErxQtyOrd.Value > 0
                ? record.ErxQtyOrd.Value
                : record.ErxQtyDispensed.HasValue && record.ErxQtyDispensed.Value > 0
                ? record.ErxQtyDispensed.Value
                : record.MmQtyOrd.HasValue && record.MmQtyOrd.Value > 0
                ? record.MmQtyOrd.Value
                : (decimal?)null;

            var sig = !string.IsNullOrWhiteSpace(record.ErxSig) ? record.ErxSig : record.MmDispensedSig;

            // Normalize quantity into dosing units (drops, puffs, sprays, insulin units, or mL)
            var dosingUnitsQty = PharmacyPackageSizeNormalizer.NormalizeToDosingUnits(erxQty, record, null, sig);

            var calculated = PharmacySigDosingCalculator.CalculateDaysSupply(dosingUnitsQty ?? erxQty, sig);
            if (calculated.HasValue && calculated.Value > 0)
                return calculated.Value;

            return record.ErxDaysSupply.HasValue && record.ErxDaysSupply.Value > 0 ? record.ErxDaysSupply.Value : calculated;
        }

        public static bool IsDaysSupplyCompliant(ErxAuditRecord? record, decimal tolerance = 0.00m, IMetaAiClient? metaAiClient = null)
        {
            if (record == null) return false;

            int? mmDays = record.MmDispensedDaysSupply;
            int? rawErxDays = record.ErxDaysSupply;
            int? erxBasedDays = CalculateErxBasedDaysSupply(record);

            if (!mmDays.HasValue && !rawErxDays.HasValue && !erxBasedDays.HasValue) return true;
            if (!mmDays.HasValue) return false;

            var tol = (int)Math.Max(0, tolerance);

            // 1. Direct match between MM dispensed days and calculated ERX based days supply
            if (erxBasedDays.HasValue && Math.Abs(mmDays.Value - erxBasedDays.Value) <= tol)
                return true;

            // 2. Direct match against raw imported ErxDaysSupply
            if (rawErxDays.HasValue && rawErxDays.Value > 0 && Math.Abs(mmDays.Value - rawErxDays.Value) <= tol)
                return true;

            // 3. Multi-Dose Vial / USP <797> Beyond-Use Date (BUD 28-35 days) or Ratio (2:1, 3:1) Compliance:
            // e.g., Testosterone Cypionate 10 mL vial: 70 days volumetric vs 35 days punctured vial limit (2:1 ratio)
            if (rawErxDays.HasValue && rawErxDays.Value > 0)
            {
                if (PharmacyPackageSizeNormalizer.IsMultiDoseVialOrRatioDaysSupplyCompliant(mmDays.Value, rawErxDays.Value, record, tolerance))
                    return true;
            }

            if (erxBasedDays.HasValue && erxBasedDays.Value > 0)
            {
                if (PharmacyPackageSizeNormalizer.IsMultiDoseVialOrRatioDaysSupplyCompliant(mmDays.Value, erxBasedDays.Value, record, tolerance))
                    return true;
            }

            if (rawErxDays.HasValue && erxBasedDays.HasValue && rawErxDays.Value > 0 && erxBasedDays.Value > 0)
            {
                if (PharmacyPackageSizeNormalizer.IsMultiDoseVialOrRatioDaysSupplyCompliant(rawErxDays.Value, erxBasedDays.Value, record, tolerance))
                    return true;
            }

            // 4. Check if pharmacy dispensed a 30-day or partial fill based on the dispensed quantity
            if (record.MmQtyDispensed.HasValue && record.MmQtyDispensed.Value > 0)
            {
                var sig = !string.IsNullOrWhiteSpace(record.ErxSig) ? record.ErxSig : record.MmDispensedSig;
                var dosingUnitsDispensed = PharmacyPackageSizeNormalizer.NormalizeToDosingUnits(record.MmQtyDispensed.Value, record, null, sig);
                var dispensedCalcDays = PharmacySigDosingCalculator.CalculateDaysSupply(dosingUnitsDispensed ?? record.MmQtyDispensed.Value, sig);
                if (dispensedCalcDays.HasValue)
                {
                    if (Math.Abs(mmDays.Value - dispensedCalcDays.Value) <= tol)
                        return true;

                    if (PharmacyPackageSizeNormalizer.IsMultiDoseVialOrRatioDaysSupplyCompliant(mmDays.Value, dispensedCalcDays.Value, record, tolerance))
                        return true;
                }
            }

            // 5. Meta AI Clinical Evaluation fallback for Days Supply
            if (metaAiClient != null && metaAiClient.IsConfigured && mmDays.HasValue)
            {
                var sig = !string.IsNullOrWhiteSpace(record.ErxSig) ? record.ErxSig : record.MmDispensedSig;
                var targetDays = erxBasedDays ?? rawErxDays ?? mmDays.Value;
                var drug = record.MmDispensedDrugName ?? record.ErxDrugName;
                var pii = ExtractPiiTokens(record);

                if (metaAiClient.IsDaysSupplyCompliant(mmDays.Value, targetDays, sig, drug, record.MmQtyDispensed, pii))
                {
                    return true;
                }
            }

            return false;
        }

        int? IErxComparisonEngine.CalculateErxBasedDaysSupply(ErxAuditRecord? record)
        {
            return CalculateErxBasedDaysSupply(record);
        }

        bool IErxComparisonEngine.IsDaysSupplyCompliant(ErxAuditRecord? record, decimal tolerance)
        {
            return IsDaysSupplyCompliant(record, tolerance, _metaAiClient);
        }

        public static bool AreNdcsMatching(ErxAuditRecord? record)
        {
            if (record == null) return false;
            return AreNdcsMatching(record.MmDispensedNdc, record.ErxNdc);
        }

        public static bool AreNdcsMatching(string? ndc1, string? ndc2)
        {
            if (string.IsNullOrWhiteSpace(ndc1) || string.IsNullOrWhiteSpace(ndc2))
                return false;

            var c1 = Regex.Replace(ndc1.Trim(), @"[^\w]", "");
            var c2 = Regex.Replace(ndc2.Trim(), @"[^\w]", "");

            if (string.IsNullOrEmpty(c1) || string.IsNullOrEmpty(c2))
                return false;

            var invalidPlaceholders = new[] { "supply", "na", "none", "unknown", "null" };
            if (invalidPlaceholders.Any(p => c1.Equals(p, StringComparison.OrdinalIgnoreCase) || c2.Equals(p, StringComparison.OrdinalIgnoreCase)))
                return false;

            // 1. Exact alphanumeric match (case-insensitive)
            if (string.Equals(c1, c2, StringComparison.OrdinalIgnoreCase))
                return true;

            // 2. Trim leading zeros
            var u1 = c1.TrimStart('0');
            var u2 = c2.TrimStart('0');
            if (!string.IsNullOrEmpty(u1) && string.Equals(u1, u2, StringComparison.OrdinalIgnoreCase))
                return true;

            // 3. Digit-only comparison (handles punctuation or embedded format differences)
            var d1 = Regex.Replace(c1, @"\D", "");
            var d2 = Regex.Replace(c2, @"\D", "");

            if (d1.Length >= 4 && d2.Length >= 4)
            {
                if (d1 == d2)
                    return true;

                var unzeroD1 = d1.TrimStart('0');
                var unzeroD2 = d2.TrimStart('0');
                if (!string.IsNullOrEmpty(unzeroD1) && unzeroD1 == unzeroD2)
                    return true;

                // 4. Standard FDA 10-to-11 digit NDC conversion
                if (d1.Length == 10 && d2.Length == 11 && Is10To11NdcMatch(d1, d2))
                    return true;
                if (d1.Length == 11 && d2.Length == 10 && Is10To11NdcMatch(d2, d1))
                    return true;
            }

            return false;
        }

        private static bool Is10To11NdcMatch(string ten, string eleven)
        {
            if (ten.Length != 10 || eleven.Length != 11) return false;
            if ("0" + ten == eleven) return true;
            if (ten.Substring(0, 5) + "0" + ten.Substring(5) == eleven) return true;
            if (ten.Substring(0, 9) + "0" + ten.Substring(9) == eleven) return true;
            return false;
        }

        private static readonly HashSet<string> KnownPatientSuffixes = new(StringComparer.OrdinalIgnoreCase)
        {
            "JR", "JR.", "SR", "SR.", "II", "III", "IV", "V", "VI", "ESQ", "MD", "DDS", "PHD", "RPH"
        };

        private static readonly HashSet<string>[] NicknameGroups = new[]
        {
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "sam", "sammy", "samuel", "samantha" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alex", "alexander", "alexandra", "alec", "lex", "lexi" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "andrew", "andy", "drew" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "anthony", "tony" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "arthur", "art", "artie" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "benjamin", "ben", "benny", "benji" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "charles", "charlie", "chuck", "chas" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "christopher", "chris", "christine", "christina", "tina" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "daniel", "dan", "danny" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "david", "dave", "davy" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "donald", "don", "donnie" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "edward", "ed", "eddie", "ted", "teddy", "edwin", "edmund" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "eugene", "gene" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "frederick", "fred", "freddie" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "gerald", "jerry", "gerry", "jerome" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "gregory", "greg" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "james", "jim", "jimmy", "jamie" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "jeffrey", "jeff", "geoffrey", "geoff" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "john", "jon", "jack", "johnny", "jonathan" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "joseph", "joe", "joey" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "kenneth", "ken", "kenny" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "lawrence", "larry", "laurence" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "matthew", "matt", "matty" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "michael", "mike", "mikey" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "nicholas", "nick", "nicky" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "patrick", "pat", "patty", "paddy" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "peter", "pete" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "phillip", "phil", "philip" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "raymond", "ray" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "richard", "rick", "ricky", "dick", "rich", "richie" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "robert", "bob", "bobby", "rob", "robbie" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ronald", "ron", "ronnie" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "stephen", "steven", "steve" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "terrence", "terry" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "thomas", "tom", "tommy" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "timothy", "tim", "timmy" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "walter", "walt", "wally" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "william", "bill", "billy", "will", "willie", "liam" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "zachary", "zach", "zack" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "albert", "al", "alan", "allen" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "barbara", "barb", "barbie" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "catherine", "katherine", "cathy", "kathy", "kate", "katie", "kay" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "deborah", "deb", "debbie", "debra" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "dorothy", "dot", "dottie" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "elizabeth", "liz", "lizzie", "beth", "betty", "betsy", "eliza", "ellie" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "frances", "fran", "frannie" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "jennifer", "jen", "jenny" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "judith", "judy" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "margaret", "maggie", "peggy", "meg", "marge" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "patricia", "pat", "patty", "trish", "tricia" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rebecca", "becky", "becca" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "susan", "sue", "suzie", "susie" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "theresa", "teresa", "terri", "tess" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "victoria", "vicky", "vicki", "tori" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "virginia", "ginny" }
        };

        private static readonly Dictionary<string, HashSet<string>> NicknameLookup = BuildNicknameLookup();

        private static Dictionary<string, HashSet<string>> BuildNicknameLookup()
        {
            var dict = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in NicknameGroups)
            {
                foreach (var name in group)
                {
                    dict[name] = group;
                }
            }
            return dict;
        }

        public static bool IsNicknameEquivalent(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
            if (NicknameLookup.TryGetValue(a, out var group) && group.Contains(b)) return true;
            return false;
        }

        private static (List<string> Tokens, string? Suffix) ParsePatientName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (new List<string>(), null);

            var cleaned = Regex.Replace(name, @"[,\.\-#/'""]", " ");
            var rawTokens = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string? suffix = null;
            var tokens = new List<string>();

            foreach (var raw in rawTokens)
            {
                var token = raw.Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(token)) continue;

                if (KnownPatientSuffixes.Contains(token))
                {
                    suffix = token.TrimEnd('.');
                }
                else
                {
                    tokens.Add(token);
                }
            }

            return (tokens, suffix);
        }

        public static bool ArePatientNamesClinicallyEquivalent(string? mmName, string? erxName, ErxAuditRecord? record = null)
        {
            if (string.IsNullOrWhiteSpace(mmName) && string.IsNullOrWhiteSpace(erxName)) return true;
            if (string.IsNullOrWhiteSpace(mmName) || string.IsNullOrWhiteSpace(erxName)) return false;

            var mmTrimmed = mmName.Trim();
            var erxTrimmed = erxName.Trim();

            if (string.Equals(mmTrimmed, erxTrimmed, StringComparison.OrdinalIgnoreCase))
                return true;

            var (tokensMm, suffixMm) = ParsePatientName(mmTrimmed);
            var (tokensErx, suffixErx) = ParsePatientName(erxTrimmed);

            // Suffix conflict check: If both have suffixes and they conflict (e.g. JR vs SR), they are different individuals
            if (!string.IsNullOrEmpty(suffixMm) && !string.IsNullOrEmpty(suffixErx))
            {
                if (!string.Equals(suffixMm, suffixErx, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Token-based matching (order-independent, nickname-aware, suffix-aware)
            if (AreTokenListsEquivalent(tokensMm, tokensErx))
                return true;

            // Cross-validation with DOB if available in record
            if (record != null && record.MmPatientDob.HasValue && record.ErxPatientDob.HasValue &&
                record.MmPatientDob.Value.Date == record.ErxPatientDob.Value.Date)
            {
                if (ArePatientNamesFuzzyOrLastNameMatching(mmTrimmed, erxTrimmed))
                    return true;
            }

            return false;
        }

        private static bool AreTokenListsEquivalent(List<string> tokens1, List<string> tokens2)
        {
            if (tokens1.Count == 0 || tokens2.Count == 0) return false;

            var smaller = tokens1.Count <= tokens2.Count ? tokens1 : tokens2;
            var larger = tokens1.Count <= tokens2.Count ? tokens2 : tokens1;

            var matchedLargerIndices = new HashSet<int>();

            foreach (var s in smaller)
            {
                var foundMatch = false;
                // Pass 1: Exact or Nickname match
                for (int i = 0; i < larger.Count; i++)
                {
                    if (matchedLargerIndices.Contains(i)) continue;
                    var l = larger[i];
                    if (string.Equals(s, l, StringComparison.OrdinalIgnoreCase) || IsNicknameEquivalent(s, l))
                    {
                        matchedLargerIndices.Add(i);
                        foundMatch = true;
                        break;
                    }
                }

                if (!foundMatch)
                {
                    // Pass 2: Initial or minor typo match
                    for (int i = 0; i < larger.Count; i++)
                    {
                        if (matchedLargerIndices.Contains(i)) continue;
                        var l = larger[i];
                        if (IsTokenOrNicknameOrInitialMatch(s, l))
                        {
                            matchedLargerIndices.Add(i);
                            foundMatch = true;
                            break;
                        }
                    }
                }

                if (!foundMatch)
                {
                    return false;
                }
            }

            // If all tokens of smaller matched:
            if (tokens1.Count == tokens2.Count)
                return true;

            // If larger has leftover tokens (e.g. single middle name or initial omitted in one system)
            var leftoverCount = larger.Count - matchedLargerIndices.Count;
            if (leftoverCount <= 1)
                return true;

            return false;
        }

        private static bool IsTokenOrNicknameOrInitialMatch(string t1, string t2)
        {
            if (string.Equals(t1, t2, StringComparison.OrdinalIgnoreCase))
                return true;

            if (IsNicknameEquivalent(t1, t2))
                return true;

            // Single initial match: e.g. "S" matches "SAM" or "SAMMY"
            if ((t1.Length == 1 && t2.StartsWith(t1, StringComparison.OrdinalIgnoreCase)) ||
                (t2.Length == 1 && t1.StartsWith(t2, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Typo tolerance: Levenshtein similarity >= 0.80 for tokens length >= 4
            if (t1.Length >= 4 && t2.Length >= 4)
            {
                if (ComputeLevenshteinSimilarity(t1, t2) >= 0.80)
                    return true;
            }

            return false;
        }

        private static bool ArePatientNamesFuzzyOrLastNameMatching(string? name1, string? name2)
        {
            if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2))
                return false;

            var (tokens1, _) = ParsePatientName(name1);
            var (tokens2, _) = ParsePatientName(name2);

            if (tokens1.Count == 0 || tokens2.Count == 0)
                return false;

            // Check if they share at least one substantial token (e.g. Last Name >= 3 chars)
            var shared = tokens1.Where(t => t.Length >= 3)
                                .Intersect(tokens2.Where(t => t.Length >= 3), StringComparer.OrdinalIgnoreCase)
                                .ToList();

            if (shared.Count > 0)
            {
                var remaining1 = tokens1.Except(shared, StringComparer.OrdinalIgnoreCase).ToList();
                var remaining2 = tokens2.Except(shared, StringComparer.OrdinalIgnoreCase).ToList();

                if (remaining1.Count == 0 || remaining2.Count == 0)
                    return true;

                if (remaining1.Any(r1 => remaining2.Any(r2 => IsTokenOrNicknameOrInitialMatch(r1, r2))))
                    return true;
            }

            var clean1 = Regex.Replace(name1, @"[^A-Za-z0-9]", "").ToUpperInvariant();
            var clean2 = Regex.Replace(name2, @"[^A-Za-z0-9]", "").ToUpperInvariant();
            return ComputeLevenshteinSimilarity(clean1, clean2) >= 0.80;
        }

        public static bool ArePatientDemographicsMatching(ErxAuditRecord? record)
        {
            if (record == null) return false;

            // 1. If Patient Names are clinically equivalent (including nickname, suffix, token order), return true
            if (ArePatientNamesClinicallyEquivalent(record.MmPatientName, record.ErxPatientName, record))
                return true;

            // 2. Patient DOB cross-validation if present in both
            var hasDob1 = record.MmPatientDob.HasValue;
            var hasDob2 = record.ErxPatientDob.HasValue;

            if (hasDob1 && hasDob2)
            {
                if (record.MmPatientDob!.Value.Date != record.ErxPatientDob!.Value.Date)
                    return false; // Different DOB = different patient!

                // DOB is identical! Check if Phone OR Address also matches
                var phoneMatches = ArePhonesSame(record.MmPatientPhone, record.ErxPatientPhone);
                var addressMatches = AreAddressesSame(record.MmPatientAddress, record.ErxPatientAddress);

                if (phoneMatches || addressMatches)
                    return true;

                // When DOB matches, if patient names share last name or fuzzy similarity, consider matched
                if (ArePatientNamesFuzzyOrLastNameMatching(record.MmPatientName, record.ErxPatientName))
                    return true;
            }
            else
            {
                // Fallback when DOB is absent: both Phone and Address must match
                if (ArePhonesSame(record.MmPatientPhone, record.ErxPatientPhone) &&
                    AreAddressesSame(record.MmPatientAddress, record.ErxPatientAddress))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool AreDobsSame(DateTime? dob1, DateTime? dob2)
        {
            if (!dob1.HasValue && !dob2.HasValue) return true;
            if (!dob1.HasValue || !dob2.HasValue) return false;
            return dob1.Value.Date == dob2.Value.Date;
        }

        public static bool ArePhonesSame(string? p1, string? p2)
        {
            if (string.IsNullOrWhiteSpace(p1) && string.IsNullOrWhiteSpace(p2)) return true;
            if (string.IsNullOrWhiteSpace(p1) || string.IsNullOrWhiteSpace(p2)) return false;

            var d1 = Regex.Replace(p1, @"\D", "");
            var d2 = Regex.Replace(p2, @"\D", "");

            if (string.IsNullOrEmpty(d1) || string.IsNullOrEmpty(d2))
                return string.Equals(p1.Trim(), p2.Trim(), StringComparison.OrdinalIgnoreCase);

            // Strip leading US country code 1
            if (d1.Length == 11 && d1.StartsWith("1")) d1 = d1.Substring(1);
            if (d2.Length == 11 && d2.StartsWith("1")) d2 = d2.Substring(1);

            return string.Equals(d1, d2, StringComparison.Ordinal);
        }

        public static bool AreAddressesSame(string? a1, string? a2)
        {
            if (string.IsNullOrWhiteSpace(a1) && string.IsNullOrWhiteSpace(a2)) return true;
            if (string.IsNullOrWhiteSpace(a1) || string.IsNullOrWhiteSpace(a2)) return false;

            if (string.Equals(a1.Trim(), a2.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            var norm1 = NormalizeAddress(a1);
            var norm2 = NormalizeAddress(a2);

            if (string.Equals(norm1, norm2, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check if one address string contains the other
            if (norm1.Contains(norm2, StringComparison.OrdinalIgnoreCase) || norm2.Contains(norm1, StringComparison.OrdinalIgnoreCase))
                return true;

            // Strip apartment/unit/suite and compare base street address
            var base1 = RemoveAddressUnit(norm1);
            var base2 = RemoveAddressUnit(norm2);

            if (!string.IsNullOrWhiteSpace(base1) && string.Equals(base1, base2, StringComparison.OrdinalIgnoreCase))
                return true;

            // Token overlap comparison
            var tokens1 = norm1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var tokens2 = norm2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var intersection = tokens1.Intersect(tokens2, StringComparer.OrdinalIgnoreCase).Count();
            var minTokens = Math.Min(tokens1.Length, tokens2.Length);

            if (minTokens > 0 && (double)intersection / minTokens >= 0.70)
                return true;

            // Levenshtein similarity fallback
            return ComputeLevenshteinSimilarity(norm1, norm2) >= 0.75;
        }

        private static string NormalizeAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return string.Empty;

            // Separate joined unit words like "RDAPT" -> "RD APT"
            var s = Regex.Replace(address, @"(?<=[A-Za-z])(?=(APT|STE|SUITE|UNIT|BLDG|#))", " ", RegexOptions.IgnoreCase);

            // Replace punctuation with spaces
            s = Regex.Replace(s, @"[,\.\-#/]", " ");

            // Standardize street suffixes
            s = Regex.Replace(s, @"\bSTREET\b", "ST", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bROAD\b", "RD", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bAVENUE\b", "AVE", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bDRIVE\b", "DR", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bBOULEVARD\b", "BLVD", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bLANE\b", "LN", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bCOURT\b", "CT", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bHIGHWAY\b", "HWY", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bPARKWAY\b", "PKWY", RegexOptions.IgnoreCase);

            // Standardize cardinal directionals
            s = Regex.Replace(s, @"\bNORTH\b", "N", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bSOUTH\b", "S", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bEAST\b", "E", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bWEST\b", "W", RegexOptions.IgnoreCase);

            // Collapse multiple whitespace
            s = Regex.Replace(s, @"\s+", " ").Trim().ToUpperInvariant();
            return s;
        }

        private static string RemoveAddressUnit(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return string.Empty;
            var s = Regex.Replace(address, @"\b(APT|STE|SUITE|UNIT|BLDG|FL|FLOOR)\s*#?\s*\w+", "", RegexOptions.IgnoreCase);
            return Regex.Replace(s, @"\s+", " ").Trim().ToUpperInvariant();
        }
    }
}
