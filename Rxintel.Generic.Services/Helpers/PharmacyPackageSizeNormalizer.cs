using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Helpers
{
    /// <summary>
    /// Clinical Pharmacy Package Size, Unit of Measure, and Manufacturer Puff/Actuation Normalizer.
    /// 
    /// Handles non-solid dosage forms:
    /// 1. Insulin: Pens (3 mL), Boxes (5 pens = 15 mL), Vials (10 mL), U-100 Units (100 units/mL).
    /// 2. Nasal Sprays ("Nozal Spress"): Fluticasone (16 g = 120 sprays), Azelastine (30 mL = 200 sprays), etc.
    /// 3. Inhalers / Puffs: Canisters vs Manufacturer Puffs (200 puffs, 120 puffs, 60 doses, 50 puffs, 30 doses) vs Gram weights (18 g, 8.5 g, 10.2 g).
    /// 4. Liquids / Solutions / Suspensions / Drops: Bottles (1 bottle = 10 mL, 60 mL, 120 mL, etc.) and Eye/Ear drops (1 mL = 20 drops).
    /// 5. Topicals: Tubes (1 tube = 15 g, 30 g, 45 g, 60 g).
    /// </summary>
    public static class PharmacyPackageSizeNormalizer
    {
        // Standard clinical liquid package sizes (mL)
        private static readonly HashSet<int> StandardLiquidPackageSizes = new()
        {
            5, 7, 8, 10, 15, 20, 25, 30, 45, 50, 60, 75, 100, 118, 120, 150, 180, 200, 237, 240, 473, 480, 500, 946, 1000
        };

        // Standard manufacturer defined puffs / actuations / doses
        private static readonly HashSet<int> StandardPuffsDoses = new()
        {
            14, 30, 50, 60, 100, 120, 140, 200, 240
        };

        // Standard topical tube package weights (grams)
        private static readonly HashSet<int> StandardTopicalPackageSizes = new()
        {
            15, 30, 45, 60, 85, 90, 100, 120
        };

        /// <summary>
        /// Determines if the drug record represents a non-solid, packaged dosage form:
        /// Insulin, Nasal Spray, Inhaler/Puff, Liquid/Solution/Drops, or Topical Cream/Ointment.
        /// </summary>
        public static bool IsNonSolidOrPackagedDrug(ErxAuditRecord? record, Medicine? med = null)
        {
            if (record == null && med == null) return false;

            return IsInsulinDrug(record, med) ||
                   IsNasalSprayDrug(record, med) ||
                   IsInhalerOrPuffDrug(record, med) ||
                   IsLiquidOrDropsDrug(record, med) ||
                   IsTopicalDrug(record, med);
        }

        public static bool IsInsulinDrug(ErxAuditRecord? record, Medicine? med = null)
        {
            var text = BuildSearchString(record, med);
            return Regex.IsMatch(text, @"\b(?:insulin|glargine|lispro|aspart|detemir|degludec|solostar|kwikpen|flexpen|flextouch|lantus|humalog|novolog|levemir|tresiba|basaglar|toujeo|fiasp|admelog|lyumjev|humulin|novolin)\b", RegexOptions.IgnoreCase);
        }

        public static bool IsNasalSprayDrug(ErxAuditRecord? record, Medicine? med = null)
        {
            var text = BuildSearchString(record, med);
            var form = med?.DosageForm ?? "";

            if (Regex.IsMatch(text, @"\b(?:nasal|nostril|nozal|flonase|fluticasone|azelastine|astepro|olopatadine|patanase|nasacort|triamcinolone|nasonex|mometasone|rhinocort|budesonide\s+nasal|ipratropium\s+nasal|atrovent\s+nasal|nasalcrom|xhance|qnasl|zetonna|dymista|ryaltris)\b", RegexOptions.IgnoreCase))
                return true;

            if (form.Contains("SPRAY", StringComparison.OrdinalIgnoreCase) && text.Contains("NASAL", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        public static bool IsInhalerOrPuffDrug(ErxAuditRecord? record, Medicine? med = null)
        {
            var text = BuildSearchString(record, med);
            var form = med?.DosageForm ?? "";

            if (Regex.IsMatch(text, @"\b(?:inhaler|inhal|inhalation|hfa|respimat|diskus|ellipta|aerosphere|flexhaler|pressair|handihaler|twisthaler|albuterol|proair|ventolin|proventil|levalbuterol|xopenex|flovent|advair|symbicort|dulera|qvar|breo|trelegy|anoro|incruse|arnuity|stiolto|combivent|spiriva|asmanex|pulmicort|wixela|airduo)\b", RegexOptions.IgnoreCase))
                return true;

            if (Regex.IsMatch(form, @"\b(?:INHALER|AEROSOL|INHALANT)\b", RegexOptions.IgnoreCase))
                return true;

            // Also check if Sig mentions puffs or inhalations
            var sig = string.Concat(record?.MmDispensedSig, " ", record?.ErxSig);
            if (Regex.IsMatch(sig, @"\b(?:puffs?|inhal(?:e|ation)|actuations?)\b", RegexOptions.IgnoreCase))
                return true;

            return false;
        }

        public static bool IsLiquidOrDropsDrug(ErxAuditRecord? record, Medicine? med = null)
        {
            var text = BuildSearchString(record, med);
            var form = med?.DosageForm ?? "";

            if (Regex.IsMatch(text, @"\b(?:solution|soln|sol|suspension|susp|liquid|liq|syrup|syr|elixir|drops?|gtts?|ophthalmic|ophth|otic|oral\s+sol|oral\s+susp|cromolyn)\b", RegexOptions.IgnoreCase))
                return true;

            if (Regex.IsMatch(form, @"\b(?:SOLUTION|SUSPENSION|LIQUID|DROPS|SYRUP|ELIXIR)\b", RegexOptions.IgnoreCase))
                return true;

            // Sig mentions drops
            var sig = string.Concat(record?.MmDispensedSig, " ", record?.ErxSig);
            if (Regex.IsMatch(sig, @"\b(?:drops?|gtts?|instill)\b", RegexOptions.IgnoreCase))
                return true;

            return false;
        }

        public static bool IsTopicalDrug(ErxAuditRecord? record, Medicine? med = null)
        {
            var text = BuildSearchString(record, med);
            var form = med?.DosageForm ?? "";

            return Regex.IsMatch(text, @"\b(?:cream|crm|ointment|oint|gel|lotion|lot|topical)\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(form, @"\b(?:CREAM|OINTMENT|GEL|LOTION)\b", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Resolves the verified metric package size (in mL or grams) from Medicine Master DB,
        /// drug names, or packaging metadata.
        /// </summary>
        public static decimal? ResolvePackageMetricSize(ErxAuditRecord? record, RxintelDbContext? dbContext)
        {
            if (record == null) return null;

            // 1. Look up MedicineMaster in database if available
            if (dbContext != null)
            {
                var med = FindMedicine(record, dbContext);
                if (med != null && !string.IsNullOrWhiteSpace(med.PackageSize))
                {
                    if (decimal.TryParse(med.PackageSize.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var pkgNum) && pkgNum > 0)
                    {
                        return pkgNum;
                    }

                    // PackageSize might be "10 mL", "60 ML", "15 GM"
                    var mPkg = Regex.Match(med.PackageSize, @"(\d+(?:\.\d+)?)");
                    if (mPkg.Success && decimal.TryParse(mPkg.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var pkgParsed) && pkgParsed > 0)
                    {
                        return pkgParsed;
                    }
                }
            }

            // 2. Extract from Drug Name strings (e.g. "60ML", "10 ML", "15 ML", "18 GM", "8.5 GM")
            var drugText = string.Concat(record.MmDispensedDrugName, " ", record.ErxDrugName);

            // Pattern: 5 x 3 mL = 15 mL (insulin box)
            var multiPackMatch = Regex.Match(drugText, @"(\d+)\s*x\s*(\d+(?:\.\d+)?)\s*(?:ml|milliliter|gm|g)\b", RegexOptions.IgnoreCase);
            if (multiPackMatch.Success &&
                decimal.TryParse(multiPackMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var count) &&
                decimal.TryParse(multiPackMatch.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var singleSize))
            {
                return count * singleSize;
            }

            // Pattern: 60 ML, 10 ML, 15 mL, 30.5 GM, 18 GM, 8.5 GM
            var volumeMatch = Regex.Match(drugText, @"\b(\d+(?:\.\d+)?)\s*(?:ml|milliliter|millilitre|cc|gm|gram|g)\b", RegexOptions.IgnoreCase);
            if (volumeMatch.Success && decimal.TryParse(volumeMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var vol) && vol > 0)
            {
                // Ensure it's not a strength like "50 mg/ml" where 50 is strength
                if (!Regex.IsMatch(volumeMatch.Value, @"\b(?:mg|mcg)/", RegexOptions.IgnoreCase))
                {
                    return vol;
                }
            }

            // Pattern: (60), (10), (15) at end of drug name or enclosed in parentheses
            var parenMatch = Regex.Match(drugText, @"\((\d+(?:\.\d+)?)\s*(?:ml|gm|g)?\)", RegexOptions.IgnoreCase);
            if (parenMatch.Success && decimal.TryParse(parenMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parenVol) && parenVol > 0)
            {
                return parenVol;
            }

            // 3. Fallback for specific well-known drug formulations
            // Cromolyn Sodium ophthalmic solution is universally 10 mL (or 13/26 mL for nasal spray)
            if (Regex.IsMatch(drugText, @"\bcromolyn\b", RegexOptions.IgnoreCase))
            {
                if (Regex.IsMatch(drugText, @"\b(?:ophth|eye|solution|soln)\b", RegexOptions.IgnoreCase))
                    return 10.0m;
                if (Regex.IsMatch(drugText, @"\bnasal\b", RegexOptions.IgnoreCase))
                    return 13.0m;
            }

            // Standard Insulin box is 15 mL (5 pens * 3 mL) or 10 mL vial
            if (IsInsulinDrug(record))
            {
                if (Regex.IsMatch(drugText, @"\b(?:solostar|kwikpen|flexpen|flextouch|pen)\b", RegexOptions.IgnoreCase))
                    return 15.0m; // 1 box = 5 pens = 15 mL
                if (Regex.IsMatch(drugText, @"\bvial\b", RegexOptions.IgnoreCase))
                    return 10.0m; // 1 vial = 10 mL
            }

            // Fluticasone nasal spray standard bottle is 16 g
            if (Regex.IsMatch(drugText, @"\b(?:flonase|fluticasone)\b", RegexOptions.IgnoreCase) && IsNasalSprayDrug(record))
            {
                return 16.0m;
            }

            // Azelastine nasal spray standard bottle is 30 mL
            if (Regex.IsMatch(drugText, @"\bazelastine\b", RegexOptions.IgnoreCase) && IsNasalSprayDrug(record))
            {
                return 30.0m;
            }

            return null;
        }

        /// <summary>
        /// Resolves the manufacturer-defined total puffs, actuations, or doses per package
        /// (e.g. 200 puffs, 120 puffs, 60 doses, 50 puffs, 30 doses).
        /// </summary>
        public static decimal? ResolveManufacturerPuffsOrDoses(ErxAuditRecord? record, RxintelDbContext? dbContext)
        {
            if (record == null) return null;

            var drugText = string.Concat(record.MmDispensedDrugName, " ", record.ErxDrugName);

            // 1. Explicit mention in drug name or package description (e.g. "200 puffs", "120 sprays", "60 doses", "50 puffs")
            var explicitMatch = Regex.Match(drugText, @"\b(\d+)\s*(?:puffs?|sprays?|actuations?|act|doses?|blisters?)\b", RegexOptions.IgnoreCase);
            if (explicitMatch.Success && decimal.TryParse(explicitMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var puffs) && puffs > 0)
            {
                return puffs;
            }

            // 2. Clinical brand catalog:
            // 200 Puffs: Albuterol HFA, ProAir, Ventolin, Proventil, Levalbuterol, Xopenex
            if (Regex.IsMatch(drugText, @"\b(?:albuterol|proair|ventolin|proventil|levalbuterol|xopenex)\b", RegexOptions.IgnoreCase))
            {
                return 200.0m;
            }

            // 120 Puffs / Actuations: Flovent HFA, Advair HFA, Symbicort, Dulera, Qvar RediHaler, Combivent Respimat
            if (Regex.IsMatch(drugText, @"\b(?:flovent\s+hfa|advair\s+hfa|symbicort|dulera|qvar|combivent)\b", RegexOptions.IgnoreCase))
            {
                return 120.0m;
            }

            // 60 Doses: Advair Diskus, Wixela Inhub, Serevent Diskus, Pulmicort Flexhaler
            if (Regex.IsMatch(drugText, @"\b(?:diskus|inhub|flexhaler|serevent)\b", RegexOptions.IgnoreCase))
            {
                return 60.0m;
            }

            // 30 Doses: Ellipta products (Trelegy, Breo, Anoro, Incruse, Arnuity), Spiriva HandiHaler/Respimat
            if (Regex.IsMatch(drugText, @"\b(?:ellipta|trelegy|breo|anoro|incruse|arnuity|spiriva)\b", RegexOptions.IgnoreCase))
            {
                return 30.0m;
            }

            // Nasal sprays:
            // Fluticasone / Flonase, Nasacort, Nasonex = 120 sprays
            if (Regex.IsMatch(drugText, @"\b(?:flonase|fluticasone|nasacort|triamcinolone|nasonex|mometasone)\b", RegexOptions.IgnoreCase) && IsNasalSprayDrug(record))
            {
                return 120.0m;
            }

            // Azelastine 30 mL = 200 sprays
            if (Regex.IsMatch(drugText, @"\bazelastine\b", RegexOptions.IgnoreCase) && IsNasalSprayDrug(record))
            {
                return 200.0m;
            }

            // Olopatadine 30.5 g = 240 sprays
            if (Regex.IsMatch(drugText, @"\b(?:olopatadine|patanase)\b", RegexOptions.IgnoreCase) && IsNasalSprayDrug(record))
            {
                return 240.0m;
            }

            // 3. Check MedicineMaster in DB for puffs / package size
            if (dbContext != null)
            {
                var med = FindMedicine(record, dbContext);
                if (med != null)
                {
                    var brandText = string.Concat(med.Brand, " ", med.Generic, " ", med.PackageSize);
                    var mMed = Regex.Match(brandText, @"\b(\d+)\s*(?:puffs?|sprays?|actuations?|act|doses?)\b", RegexOptions.IgnoreCase);
                    if (mMed.Success && decimal.TryParse(mMed.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var medPuffs) && medPuffs > 0)
                    {
                        return medPuffs;
                    }

                    if (decimal.TryParse(med.PackageSize, NumberStyles.Any, CultureInfo.InvariantCulture, out var pNum) && StandardPuffsDoses.Contains((int)pNum))
                    {
                        return pNum;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Evaluates whether two quantities are clinically equivalent under package size,
        /// unit-of-measure, or manufacturer puff/actuation conversions.
        /// 
        /// Examples:
        /// 1. Physician wrote 1 bottle, Pharmacist entered 60 mL -> TRUE (60 == 1 * 60)
        /// 2. Physician wrote 1 bottle, Pharmacist entered 10 mL (Cromolyn) -> TRUE (10 == 1 * 10)
        /// 3. Physician wrote 200 puffs, Pharmacist entered 1 (inhaler) or 18 g -> TRUE
        /// 4. Physician wrote 50 puffs, Pharmacist entered 1 (inhaler) -> TRUE
        /// 5. Physician wrote 1 box, Pharmacist entered 15 mL or 1500 units (Insulin) -> TRUE
        /// </summary>
        public static bool AreQuantitiesPackageEquivalent(
            decimal qtyA,
            decimal qtyB,
            ErxAuditRecord? record,
            RxintelDbContext? dbContext,
            decimal tolerance = 0.00m)
        {
            // 1. Direct equality within tolerance
            if (Math.Abs(qtyA - qtyB) <= tolerance)
                return true;

            // 2. Must be non-solid / packaged drug form (Insulin, Nasal Spray, Inhaler/Puff, Liquid/Drops, Topical)
            // Solid oral tablets/capsules are NOT package-equivalent (e.g. 1 tablet is never 60 tablets).
            if (!IsNonSolidOrPackagedDrug(record))
                return false;

            var minQty = Math.Min(qtyA, qtyB);
            var maxQty = Math.Max(qtyA, qtyB);

            if (minQty <= 0) return false;

            // 3. Package Size Multiplier Check: MaxQty == MinQty * PackageMetricSize
            // e.g. 1 bottle vs 60 mL, 2 bottles vs 120 mL, 1 bottle vs 10 mL
            var pkgSize = ResolvePackageMetricSize(record, dbContext);
            if (pkgSize.HasValue && pkgSize.Value > 0)
            {
                var expectedMetricQty = minQty * pkgSize.Value;
                var effectiveTol = tolerance > 0 ? tolerance : 0.02m * maxQty;
                if (Math.Abs(maxQty - expectedMetricQty) <= effectiveTol)
                    return true;
            }

            // 4. Manufacturer Puffs / Actuations / Doses Check
            // e.g. 1 inhaler vs 200 puffs, 2 inhalers vs 400 puffs, 1 canister vs 50 puffs
            var puffs = ResolveManufacturerPuffsOrDoses(record, dbContext);
            if (puffs.HasValue && puffs.Value > 0)
            {
                var expectedPuffs = minQty * puffs.Value;
                var effectiveTol = tolerance > 0 ? tolerance : 0.02m * maxQty;
                if (Math.Abs(maxQty - expectedPuffs) <= effectiveTol)
                    return true;
            }

            // 5. Inhaler Gram Weight vs Puffs Equivalence:
            // e.g. 18 g canister has 200 puffs; 8.5 g canister has 200 puffs; 10.2 g has 120 puffs; 16 g has 120 sprays
            if (IsInhalerOrPuffDrug(record) || IsNasalSprayDrug(record))
            {
                // Canister count (1, 2, 3...) vs standard puffs (200, 120, 60, 50, 30)
                if (IsIntegerPackageCount(minQty) && StandardPuffsDoses.Contains((int)Math.Round(maxQty / minQty)))
                {
                    return true;
                }

                // Metric weight in grams (18, 8.5, 6.7, 10.2, 10.6, 12, 14, 16) vs puffs (200, 120, 60, 50)
                if (IsWeightPuffEquivalent(qtyA, qtyB))
                {
                    return true;
                }
            }

            // 6. Insulin Unit / Volume / Package Equivalence:
            // 1 box = 15 mL = 5 pens = 1500 units (U-100)
            // 1 vial = 10 mL = 1000 units (U-100)
            // 1 pen = 3 mL = 300 units (U-100)
            if (IsInsulinDrug(record))
            {
                if (IsInsulinEquivalent(qtyA, qtyB))
                    return true;
            }

            // 7. General Ratio Validation for Recognized Package Sizes:
            // If minQty is an integer package count (1, 2, 3... bottles/tubes/canisters),
            // does the ratio match a known clinical package volume or count?
            if (IsIntegerPackageCount(minQty))
            {
                var ratio = maxQty / minQty;
                var roundedRatio = (int)Math.Round(ratio);

                if (Math.Abs(ratio - roundedRatio) <= 0.05m)
                {
                    if (IsLiquidOrDropsDrug(record) && StandardLiquidPackageSizes.Contains(roundedRatio))
                        return true;

                    if (IsInhalerOrPuffDrug(record) && StandardPuffsDoses.Contains(roundedRatio))
                        return true;

                    if (IsNasalSprayDrug(record) && (StandardLiquidPackageSizes.Contains(roundedRatio) || StandardPuffsDoses.Contains(roundedRatio)))
                        return true;

                    if (IsTopicalDrug(record) && StandardTopicalPackageSizes.Contains(roundedRatio))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Converts a prescribed or dispensed quantity into total countable dosing units
        /// (drops, puffs, sprays, insulin units, or mL) for accurate Days Supply calculation.
        /// </summary>
        public static decimal? NormalizeToDosingUnits(
            decimal? quantity,
            ErxAuditRecord? record,
            RxintelDbContext? dbContext,
            string? sig)
        {
            if (!quantity.HasValue || quantity.Value <= 0) return null;

            var val = quantity.Value;
            var cleanSig = sig?.Trim().ToLowerInvariant() ?? "";

            // 1. Ophthalmic / Otic Drops:
            // Standard clinical pharmacy conversion: 1 mL ≈ 20 drops (or 15-20 drops/mL).
            if (Regex.IsMatch(cleanSig, @"\b(?:drops?|gtts?|instill)\b", RegexOptions.IgnoreCase) ||
                (IsLiquidOrDropsDrug(record) && Regex.IsMatch(cleanSig, @"\b(?:eye|eyes|ear|ears|ou|od|os|au|ad|as)\b", RegexOptions.IgnoreCase)))
            {
                // If quantity is small package count (<= 10 bottles), convert bottles -> mL
                decimal metricMl = val;
                if (val <= 10.0m)
                {
                    var pkgSize = ResolvePackageMetricSize(record, dbContext) ?? 10.0m; // Default to 10 mL for eye drops if unspecified
                    metricMl = val * pkgSize;
                }

                // Convert mL to drops (20 drops / mL)
                return metricMl * 20.0m;
            }

            // 2. Inhalers / Puffs:
            // Sig is in puffs or inhalations (e.g. "take 2 puffs bid")
            if (Regex.IsMatch(cleanSig, @"\b(?:puffs?|inhal(?:e|ation)|actuations?)\b", RegexOptions.IgnoreCase) || IsInhalerOrPuffDrug(record))
            {
                // If quantity is already total puffs (>= 20), keep as puffs
                if (val >= 20.0m)
                {
                    return val;
                }

                // Quantity is canister count (e.g. 1, 2) or gram weight (e.g. 18 g, 8.5 g)
                var puffsPerCanister = ResolveManufacturerPuffsOrDoses(record, dbContext) ?? 200.0m;

                if (val <= 10.0m)
                {
                    // Small integer canister count (1, 2 canisters)
                    return val * puffsPerCanister;
                }
                else
                {
                    // Gram weight (18g, 8.5g, etc.) -> 1 canister worth of puffs
                    return puffsPerCanister;
                }
            }

            // 3. Nasal Sprays:
            // Sig is in sprays (e.g. "1 spray each nostril daily")
            if (Regex.IsMatch(cleanSig, @"\b(?:sprays?|spr|nostril)\b", RegexOptions.IgnoreCase) || IsNasalSprayDrug(record))
            {
                if (val >= 30.0m)
                {
                    return val; // Already sprays
                }

                var spraysPerBottle = ResolveManufacturerPuffsOrDoses(record, dbContext) ?? 120.0m;
                if (val <= 10.0m)
                {
                    return val * spraysPerBottle;
                }
                else
                {
                    // Gram weight (16g, etc.) -> 1 bottle worth of sprays
                    return spraysPerBottle;
                }
            }

            // 4. Insulin:
            // Sig is in units (e.g. "inject 20 units daily")
            if (Regex.IsMatch(cleanSig, @"\b(?:units?|u)\b", RegexOptions.IgnoreCase) || IsInsulinDrug(record))
            {
                // If already in insulin units (e.g. 300, 1000, 1500 units)
                if (val >= 50.0m)
                {
                    return val;
                }

                // If quantity is volume in mL (e.g. 10 mL vial = 1000 units, 15 mL box = 1500 units, 3 mL pen = 300 units)
                if (val > 10.0m && val <= 50.0m)
                {
                    return val * 100.0m; // U-100 = 100 units/mL
                }

                // If quantity is small box/package count (1, 2 boxes)
                var pkgSizeMl = ResolvePackageMetricSize(record, dbContext) ?? 15.0m; // Default 15 mL box
                return (val * pkgSizeMl) * 100.0m;
            }

            // 5. Oral Liquids (mL, tsp, tbsp):
            // If quantity is small package count (1, 2 bottles) of a liquid, convert packages to mL
            if (IsLiquidOrDropsDrug(record) && val <= 10.0m)
            {
                var pkgSizeMl = ResolvePackageMetricSize(record, dbContext);
                if (pkgSizeMl.HasValue && pkgSizeMl.Value > 0)
                {
                    return val * pkgSizeMl.Value;
                }
            }

            return val;
        }

        /// <summary>
        /// Normalizes ordered quantity to the unit system of dispensed quantity
        /// so that Dispensed <= Ordered comparison works across packages and units.
        /// </summary>
        public static decimal NormalizeOrderedToDispensedUnits(
            decimal ordered,
            decimal dispensed,
            ErxAuditRecord? record,
            RxintelDbContext? dbContext)
        {
            if (AreQuantitiesPackageEquivalent(ordered, dispensed, record, dbContext))
            {
                // If they are package-equivalent, ordered matches dispensed
                return dispensed;
            }

            // If ordered is package count (e.g. 1) and dispensed is metric (e.g. 60 mL), convert ordered to metric
            var pkgSize = ResolvePackageMetricSize(record, dbContext);
            if (pkgSize.HasValue && pkgSize.Value > 0 && ordered <= 10.0m && dispensed > 10.0m)
            {
                return ordered * pkgSize.Value;
            }

            // If ordered is package count and dispensed is puffs
            var puffs = ResolveManufacturerPuffsOrDoses(record, dbContext);
            if (puffs.HasValue && puffs.Value > 0 && ordered <= 10.0m && dispensed > 10.0m)
            {
                return ordered * puffs.Value;
            }

            return ordered;
        }

        private static bool IsIntegerPackageCount(decimal qty)
        {
            if (qty <= 0 || qty > 20) return false;
            return Math.Abs(qty - Math.Round(qty)) < 0.001m;
        }

        private static bool IsWeightPuffEquivalent(decimal qtyA, decimal qtyB)
        {
            var min = Math.Min(qtyA, qtyB);
            var max = Math.Max(qtyA, qtyB);

            // Albuterol / Ventolin / ProAir: 18 g or 8.5 g or 6.7 g <-> 200 puffs
            if ((Math.Abs(min - 18.0m) < 0.5m || Math.Abs(min - 8.5m) < 0.5m || Math.Abs(min - 6.7m) < 0.5m) &&
                Math.Abs(max - 200.0m) < 1.0m)
            {
                return true;
            }

            // Symbicort / Flovent / Qvar: 10.2 g, 10.6 g, 12 g, 8.8 g <-> 120 puffs
            if ((Math.Abs(min - 10.2m) < 0.5m || Math.Abs(min - 10.6m) < 0.5m || Math.Abs(min - 12.0m) < 0.5m || Math.Abs(min - 8.8m) < 0.5m) &&
                Math.Abs(max - 120.0m) < 1.0m)
            {
                return true;
            }

            // Nasal spray: 16 g <-> 120 sprays
            if (Math.Abs(min - 16.0m) < 0.5m && Math.Abs(max - 120.0m) < 1.0m)
            {
                return true;
            }

            // Nasal spray: 30.5 g <-> 240 sprays
            if (Math.Abs(min - 30.5m) < 0.5m && Math.Abs(max - 240.0m) < 1.0m)
            {
                return true;
            }

            return false;
        }

        private static bool IsInsulinEquivalent(decimal qtyA, decimal qtyB)
        {
            var min = Math.Min(qtyA, qtyB);
            var max = Math.Max(qtyA, qtyB);

            // 1 box = 15 mL = 1500 units (U-100)
            if (IsIntegerPackageCount(min))
            {
                if (Math.Abs(max - (min * 15.0m)) < 0.1m) return true;     // 1 box == 15 mL
                if (Math.Abs(max - (min * 10.0m)) < 0.1m) return true;     // 1 vial == 10 mL
                if (Math.Abs(max - (min * 3.0m)) < 0.1m) return true;      // 1 pen == 3 mL
                if (Math.Abs(max - (min * 5.0m)) < 0.1m) return true;      // 1 box == 5 pens
                if (Math.Abs(max - (min * 1500.0m)) < 1.0m) return true;  // 1 box == 1500 units
                if (Math.Abs(max - (min * 1000.0m)) < 1.0m) return true;  // 1 vial == 1000 units
                if (Math.Abs(max - (min * 300.0m)) < 1.0m) return true;   // 1 pen == 300 units
            }

            // Volume (mL) vs Units (100 units / mL)
            // e.g. 15 mL == 1500 units; 10 mL == 1000 units; 3 mL == 300 units; 9 mL == 900 units
            if (Math.Abs(max - (min * 100.0m)) < 1.0m)
            {
                return true;
            }

            return false;
        }

        private static readonly ConcurrentDictionary<string, Medicine?> MedicineCache = new(StringComparer.OrdinalIgnoreCase);

        private static Medicine? FindMedicine(ErxAuditRecord record, RxintelDbContext dbContext)
        {
            var cacheKey = $"{record.MmDispensedNdc}|{record.ErxNdc}|{record.MmDispensedDrugName}|{record.ErxDrugName}";
            if (MedicineCache.TryGetValue(cacheKey, out var cached))
                return cached;

            try
            {
                // 1. Diabetic supplies and supply items never exist as drug packages in MedicineMasters - short circuit in 0ms
                var nameMm = record.MmDispensedDrugName ?? string.Empty;
                var nameErx = record.ErxDrugName ?? string.Empty;
                var combinedNames = $"{nameMm} {nameErx}";
                if (Regex.IsMatch(combinedNames, @"\b(lancet|needle|test\s*strip|syringe|pen\s*ndl|supply)\b", RegexOptions.IgnoreCase))
                {
                    MedicineCache[cacheKey] = null;
                    return null;
                }

                // 2. Fast cached NDC lookups using indexed queries and AsNoTracking
                var medMm = PharmacyDrugNormalizer.FindMedicineInMaster(record.MmDispensedNdc, dbContext);
                if (medMm != null)
                {
                    MedicineCache[cacheKey] = medMm;
                    return medMm;
                }

                var medErx = PharmacyDrugNormalizer.FindMedicineInMaster(record.ErxNdc, dbContext);
                if (medErx != null)
                {
                    MedicineCache[cacheKey] = medErx;
                    return medErx;
                }
            }
            catch
            {
                // Graceful fallback if DB is unreachable or disconnected
            }

            MedicineCache[cacheKey] = null;
            return null;
        }

        private static string NormalizeNdc(string? ndc)
        {
            if (string.IsNullOrWhiteSpace(ndc)) return string.Empty;
            return Regex.Replace(ndc.Trim(), @"[^\w]", "");
        }

        private static string BuildSearchString(ErxAuditRecord? record, Medicine? med)
        {
            return string.Concat(
                record?.MmDispensedDrugName, " ",
                record?.ErxDrugName, " ",
                record?.MmPotencyUnitCode, " ",
                record?.ErxPotencyUnitCode, " ",
                med?.Brand, " ",
                med?.Generic, " ",
                med?.DosageForm, " ",
                med?.PackageSize
            ).ToUpperInvariant();
        }

        /// <summary>
        /// Checks whether a prescription record represents a multi-dose vial, injectable solution/suspension,
        /// or liquid container governed by USP &lt;797&gt; beyond-use-date (BUD) puncture discard rules.
        /// </summary>
        public static bool IsMultiDoseVialOrInjectable(ErxAuditRecord? record)
        {
            if (record == null) return false;

            var drug = string.Concat(record.MmDispensedDrugName, " ", record.ErxDrugName).ToLowerInvariant();
            var form = string.Concat(record.MmPotencyUnitCode, " ", record.ErxPotencyUnitCode).ToLowerInvariant();
            var sig = string.Concat(record.MmDispensedSig, " ", record.ErxSig).ToLowerInvariant();

            // 1. Common multidose / injectable medications (Testosterone, Insulin, Vitamin B12, HCG, etc.)
            if (drug.Contains("testosterone") ||
                drug.Contains("insulin") ||
                drug.Contains("cyanocobalamin") ||
                drug.Contains("b-12") ||
                drug.Contains("b12") ||
                drug.Contains("chorionic") ||
                drug.Contains("hcg") ||
                drug.Contains("medroxyprogesterone") ||
                drug.Contains("depo") ||
                drug.Contains("somatropin") ||
                drug.Contains("growth hormone") ||
                drug.Contains("leuprolide") ||
                drug.Contains("lupron") ||
                drug.Contains("methotrexate") ||
                drug.Contains("lovenox") ||
                drug.Contains("enoxaparin") ||
                drug.Contains("heparin"))
            {
                return true;
            }

            // 2. Dosage forms / potency unit codes
            if (form.Contains("inj") ||
                form.Contains("vial") ||
                form.Contains("mdv") ||
                form.Contains("ml") ||
                form.Contains("solution") ||
                form.Contains("suspension") ||
                form.Contains("drops") ||
                form.Contains("gtt"))
            {
                return true;
            }

            // 3. Sig keywords indicating injection, multi-dose vial, or sterile puncture
            if (Regex.IsMatch(sig, @"\b(?:inject|im|sc|sq|subq|intramuscular|subcutaneous|multidose|vial|drops?|gtts?|instill)\b", RegexOptions.IgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether two Days Supply values (e.g., 70 vs 35, 56 vs 28, 60 vs 30, 84 vs 28, 90 vs 30)
        /// are clinically and pharmaceutically equivalent under Multi-Dose Vial (MDV) dispensing rules,
        /// USP &lt;797&gt; 28-35 day punctured container beyond-use-date (BUD) limits, or integer multiple ratios (2:1, 1:2, 3:1, 1:3).
        /// </summary>
        public static bool IsMultiDoseVialOrRatioDaysSupplyCompliant(
            int days1,
            int days2,
            ErxAuditRecord? record,
            decimal tolerance = 0.00m)
        {
            if (days1 <= 0 || days2 <= 0) return false;

            var tol = (int)Math.Max(0, tolerance);
            if (Math.Abs(days1 - days2) <= tol) return true;

            var maxDays = Math.Max(days1, days2);
            var minDays = Math.Min(days1, days2);

            // Clean 2:1 ratio (e.g., 70 vs 35, 60 vs 30, 56 vs 28, 30 vs 15)
            // Mathematical volumetric days supply vs 1-month / 28-35 day multi-dose vial duration
            var doubleDiff = Math.Abs(maxDays - (minDays * 2));
            if (doubleDiff <= (tol > 0 ? tol : 1))
            {
                return true;
            }

            // Clean 3:1 ratio (e.g., 84 vs 28, 90 vs 30)
            // 3-month volumetric supply vs 1-month punctured multi-dose vial duration
            var tripleDiff = Math.Abs(maxDays - (minDays * 3));
            if (tripleDiff <= (tol > 0 ? tol : 1))
            {
                return true;
            }

            // USP <797> 28-35 Day Multi-Dose Vial Beyond-Use-Date (BUD) Rule:
            // Standard multi-dose vials (like Testosterone, Insulin, HCG) must be discarded within 28-35 days after puncture.
            // If the shorter days supply is in the 28-35 day puncture window and the longer days supply
            // is a standard volumetric capacity for a multi-dose package (40-120 days):
            if (minDays >= 28 && minDays <= 35)
            {
                if (IsMultiDoseVialOrInjectable(record) || IsLiquidOrDropsDrug(record) || IsInhalerOrPuffDrug(record) || IsNasalSprayDrug(record))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
