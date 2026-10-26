using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Helpers
{
    /// <summary>
    /// Represents normalized components extracted from a clinical drug string.
    /// </summary>
    public record ParsedDrugComponents(
        HashSet<string> Ingredients,
        List<decimal> Strengths,
        string? DosageForm,
        bool IsExtendedRelease,
        string RawCleaned
    );

    /// <summary>
    /// Clinical representation of a Medicine Master match.
    /// </summary>
    public class MedicineMasterLookupResult
    {
        public bool IsMatched { get; set; }
        public string? DrugName { get; set; }
        public string? GenericName { get; set; }
        public string? DosageForm { get; set; }
        public string? Strength { get; set; }
        public string? StrengthUnit { get; set; }
        public string? PackageSize { get; set; }
        public string? Ndc { get; set; }
        public string? MatchReason { get; set; }
    }

    /// <summary>
    /// USA Pharmacy Drug Name Normalizer, Clinical Equivalence Evaluator,
    /// and Medicine Master Reconciliation Engine.
    /// 
    /// Note on Medicine Master:
    /// In Medicine Master, "/" and "\" characters have been eliminated for DrugName / BrandName.
    /// This engine sanitizes all incoming drug strings by eliminating "/" and "\" to ensure
    /// accurate cross-referencing and resolves abbreviations, dosage forms, strengths, and package sizes.
    /// </summary>
    public static class PharmacyDrugNormalizer
    {
        private static readonly Dictionary<string, string> StandardAbbreviations = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ACETAMIN", "acetaminophen" },
            { "ACET", "acetaminophen" },
            { "APAP", "acetaminophen" },
            { "HCTZ", "hydrochlorothiazide" },
            { "SMZ", "sulfamethoxazole" },
            { "TMP", "trimethoprim" },
            { "ASA", "aspirin" },
            { "AMOX", "amoxicillin" },
            { "CLAV", "clavulanate" },
            { "IBU", "ibuprofen" },
            { "METOPR", "metoprolol" },
            { "SUCC", "succinate" },
            { "TART", "tartrate" },
            { "PRAVA", "pravastatin" },
            { "ATORV", "atorvastatin" },
            { "SIMVA", "simvastatin" },
            { "WARFA", "warfarin" }
        };

        private static readonly Dictionary<string, string> BrandToGenericMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // Analgesics / Opioids / Muscle Relaxants
            { "xanax", "alprazolam" },
            { "norco", "hydrocodone;acetaminophen" },
            { "vicodin", "hydrocodone;acetaminophen" },
            { "lortab", "hydrocodone;acetaminophen" },
            { "percocet", "oxycodone;acetaminophen" },
            { "endocet", "oxycodone;acetaminophen" },
            { "roxyicodone", "oxycodone" },
            { "oxycontin", "oxycodone" },
            { "ms contin", "morphine" },
            { "dilaudid", "hydromorphone" },
            { "ultram", "tramadol" },
            { "tylenol", "acetaminophen" },
            { "advil", "ibuprofen" },
            { "motrin", "ibuprofen" },
            { "aleve", "naproxen" },
            { "naprosyn", "naproxen" },
            { "mobic", "meloxicam" },
            { "celebrex", "celecoxib" },
            { "toradol", "ketorolac" },
            { "relafen", "nabumetone" },
            { "soma", "carisoprodol" },
            { "flexeril", "cyclobenzaprine" },
            { "lioresal", "baclofen" },
            { "zanaflex", "tizanidine" },
            { "robaxin", "methocarbamol" },

            // Insulins & Diabetes Biologics (including interchangeable biosimilars)
            { "lantus", "insulin;glargine" },
            { "solostar", "insulin;glargine" },
            { "basaglar", "insulin;glargine" },
            { "toujeo", "insulin;glargine" },
            { "semglee", "insulin;glargine" },
            { "rezvoglar", "insulin;glargine" },
            { "novolog", "insulin;aspart" },
            { "fiasp", "insulin;aspart" },
            { "humalog", "insulin;lispro" },
            { "admelog", "insulin;lispro" },
            { "lyumjev", "insulin;lispro" },
            { "levemir", "insulin;detemir" },
            { "tresiba", "insulin;degludec" },
            { "humulin", "insulin;isophane" },
            { "novolin", "insulin;isophane" },
            { "victoza", "liraglutide" },
            { "saxenda", "liraglutide" },
            { "ozempic", "semaglutide" },
            { "rybelsus", "semaglutide" },
            { "wegovy", "semaglutide" },
            { "mounjaro", "tirzepatide" },
            { "zepbound", "tirzepatide" },
            { "trulicity", "dulaglutide" },
            { "byetta", "exenatide" },
            { "bydureon", "exenatide" },
            { "jardiance", "empagliflozin" },
            { "farxiga", "dapagliflozin" },
            { "invokana", "canagliflozin" },
            { "januvia", "sitagliptin" },
            { "tradjenta", "linagliptin" },
            { "onglyza", "saxagliptin" },
            { "actos", "pioglitazone" },
            { "amaryl", "glimepiride" },
            { "glucotrol", "glipizide" },
            { "glucophage", "metformin" },
            { "jentadueto", "linagliptin;metformin" },
            { "janumet", "sitagliptin;metformin" },
            { "synjardy", "empagliflozin;metformin" },
            { "xigduo", "dapagliflozin;metformin" },

            // Cardiovascular / Antihypertensives / Anticoagulants
            { "lipitor", "atorvastatin" },
            { "zocor", "simvastatin" },
            { "pravachol", "pravastatin" },
            { "crestor", "rosuvastatin" },
            { "toprol", "metoprolol" },
            { "toprol xl", "metoprolol" },
            { "lopressor", "metoprolol" },
            { "tenormin", "atenolol" },
            { "coreg", "carvedilol" },
            { "bystolic", "nebivolol" },
            { "norvasc", "amlodipine" },
            { "lotrel", "amlodipine;benazepril" },
            { "prinivil", "lisinopril" },
            { "zestril", "lisinopril" },
            { "zestoretic", "lisinopril;hydrochlorothiazide" },
            { "cozaar", "losartan" },
            { "hyzaar", "losartan;hydrochlorothiazide" },
            { "diovan", "valsartan" },
            { "benicar", "olmesartan" },
            { "entresto", "sacubitril;valsartan" },
            { "cardizem", "diltiazem" },
            { "tiazac", "diltiazem" },
            { "calan", "verapamil" },
            { "verelan", "verapamil" },
            { "procardia", "nifedipine" },
            { "adalat", "nifedipine" },
            { "apresoline", "hydralazine" },
            { "imdur", "isosorbide" },
            { "isordil", "isosorbide" },
            { "nitrostat", "nitroglycerin" },
            { "lasix", "furosemide" },
            { "bumex", "bumetanide" },
            { "demadex", "torsemide" },
            { "microzide", "hydrochlorothiazide" },
            { "hygroton", "chlorthalidone" },
            { "aldactone", "spironolactone" },
            { "dyazide", "triamterene;hydrochlorothiazide" },
            { "maxzide", "triamterene;hydrochlorothiazide" },
            { "coumadin", "warfarin" },
            { "jantoven", "warfarin" },
            { "plavix", "clopidogrel" },
            { "eliquis", "apixaban" },
            { "xarelto", "rivaroxaban" },
            { "brilinta", "ticagrelor" },
            { "pradaxa", "dabigatran" },
            { "lanoxin", "digoxin" },
            { "cordarone", "amiodarone" },
            { "pacerone", "amiodarone" },

            // Respiratory & Allergy
            { "proair", "albuterol" },
            { "ventolin", "albuterol" },
            { "proventil", "albuterol" },
            { "xopenex", "levalbuterol" },
            { "advair", "fluticasone;salmeterol" },
            { "symbicort", "budesonide;formoterol" },
            { "breo", "fluticasone;vilanterol" },
            { "trelegy", "fluticasone;umeclidinium;vilanterol" },
            { "spiriva", "tiotropium" },
            { "atrovent", "ipratropium" },
            { "combivent", "ipratropium;albuterol" },
            { "duoneb", "ipratropium;albuterol" },
            { "pulmicort", "budesonide" },
            { "flovent", "fluticasone" },
            { "flonase", "fluticasone" },
            { "qvar", "beclomethasone" },
            { "asmanex", "mometasone" },
            { "singulair", "montelukast" },
            { "claritin", "loratadine" },
            { "zyrtec", "cetirizine" },
            { "xyzal", "levocetirizine" },
            { "allegra", "fexofenadine" },
            { "atarax", "hydroxyzine" },
            { "vistaril", "hydroxyzine" },
            { "benadryl", "diphenhydramine" },

            // Neuro / Psych / Anticonvulsants
            { "neurontin", "gabapentin" },
            { "lyrica", "pregabalin" },
            { "cymbalta", "duloxetine" },
            { "effexor", "venlafaxine" },
            { "pristiq", "desvenlafaxine" },
            { "prozac", "fluoxetine" },
            { "zoloft", "sertraline" },
            { "paxil", "paroxetine" },
            { "lexapro", "escitalopram" },
            { "celexa", "citalopram" },
            { "luvox", "fluvoxamine" },
            { "wellbutrin", "bupropion" },
            { "zyban", "bupropion" },
            { "remeron", "mirtazapine" },
            { "desyrel", "trazodone" },
            { "elavil", "amitriptyline" },
            { "adderall", "dextroamphetamine;amphetamine" },
            { "vyvanse", "lisdexamfetamine" },
            { "ritalin", "methylphenidate" },
            { "concerta", "methylphenidate" },
            { "focalin", "dexmethylphenidate" },
            { "strattera", "atomoxetine" },
            { "klonopin", "clonazepam" },
            { "ativan", "lorazepam" },
            { "valium", "diazepam" },
            { "restoril", "temazepam" },
            { "ambien", "zolpidem" },
            { "lunesta", "eszopiclone" },
            { "seroquel", "quetiapine" },
            { "zyprexa", "olanzapine" },
            { "risperdal", "risperidone" },
            { "invega", "paliperidone" },
            { "abilify", "aripiprazole" },
            { "geodon", "ziprasidone" },
            { "latuda", "lurasidone" },
            { "vraylar", "cariprazine" },
            { "depakote", "divalproex" },
            { "depakene", "valproic" },
            { "tegretol", "carbamazepine" },
            { "carbatrol", "carbamazepine" },
            { "trileptal", "oxcarbazepine" },
            { "oxellar", "oxcarbazepine" },
            { "lamictal", "lamotrigine" },
            { "keppra", "levetiracetam" },
            { "topamax", "topiramate" },
            { "trokendi", "topiramate" },
            { "vimpat", "lacosamide" },
            { "dilantin", "phenytoin" },
            { "sinemet", "carbidopa;levodopa" },
            { "mirapex", "pramipexole" },
            { "requip", "ropinirole" },
            { "aricept", "donepezil" },
            { "namenda", "memantine" },

            // Topicals / Patches
            { "lidoderm", "lidocaine" },
            { "ztlido", "lidocaine" },
            { "voltaren", "diclofenac" },
            { "flector", "diclofenac" },
            { "pennsaid", "diclofenac" },
            { "aldara", "imiquimod" },

            // GI / Urology / Endocrine / Anti-infectives
            { "synthroid", "levothyroxine" },
            { "levoxyl", "levothyroxine" },
            { "tirosint", "levothyroxine" },
            { "armour", "thyroid" },
            { "cytomel", "liothyronine" },
            { "prilosec", "omeprazole" },
            { "nexium", "esomeprazole" },
            { "prevacid", "lansoprazole" },
            { "protonix", "pantoprazole" },
            { "aciphex", "rabeprazole" },
            { "dexilant", "dexlansoprazole" },
            { "pepcid", "famotidine" },
            { "carafate", "sucralfate" },
            { "zofran", "ondansetron" },
            { "phenergan", "promethazine" },
            { "reglan", "metoclopramide" },
            { "colcrys", "colchicine" },
            { "zyloprim", "allopurinol" },
            { "uloric", "febuxostat" },
            { "flomax", "tamsulosin" },
            { "proscar", "finasteride" },
            { "propecia", "finasteride" },
            { "avodart", "dutasteride" },
            { "cialis", "tadalafil" },
            { "viagra", "sildenafil" },
            { "myrbetriq", "mirabegron" },
            { "gemtesa", "vibegron" },
            { "ditropan", "oxybutynin" },
            { "vesicare", "solifenacin" },
            { "augmentin", "amoxicillin;clavulanate" },
            { "zithromax", "azithromycin" },
            { "cipro", "ciprofloxacin" },
            { "bactrim", "sulfamethoxazole;trimethoprim" },
            { "diflucan", "fluconazole" },
            { "keflex", "cephalexin" },
            { "flagyl", "metronidazole" },
            { "cleocin", "clindamycin" },
            { "valtrex", "valacyclovir" },
            { "zovirax", "acyclovir" },
            { "prezcobix", "darunavir;cobicistat" },
            { "biktarvy", "bictegravir;emtricitabine;tenofovir" },
            { "triumeq", "abacavir;dolutegravir;lamivudine" },
            { "symtuza", "darunavir;cobicistat;emtricitabine;tenofovir" },
            { "epclusa", "sofosbuvir;velpatasvir" },
            { "mavyret", "glecaprevir;pibrentasvir" },
            { "aimovig", "erenumab" },
            { "emgality", "galcanezumab" },
            { "ajovy", "fremanezumab" },
            { "nurtec", "rimegepant" },
            { "ubrelvy", "ubrogepant" },
            { "boniva", "ibandronate" },
            { "fosamax", "alendronate" },
            { "actonel", "risedronate" },
            { "prolia", "denosumab" }
        };

        private static readonly Dictionary<string, string> DosageFormMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "tab", "TABLET" },
            { "tabs", "TABLET" },
            { "tablet", "TABLET" },
            { "tablets", "TABLET" },
            { "cap", "CAPSULE" },
            { "caps", "CAPSULE" },
            { "capsule", "CAPSULE" },
            { "capsules", "CAPSULE" },
            { "sol", "SOLUTION" },
            { "soln", "SOLUTION" },
            { "solution", "SOLUTION" },
            { "syrup", "SOLUTION" },
            { "elixir", "SOLUTION" },
            { "susp", "SUSPENSION" },
            { "suspension", "SUSPENSION" },
            { "crm", "CREAM" },
            { "cream", "CREAM" },
            { "oint", "OINTMENT" },
            { "ointment", "OINTMENT" },
            { "inj", "INJECTION" },
            { "injection", "INJECTION" },
            { "drop", "DROPS" },
            { "drops", "DROPS" },
            { "patch", "PATCH" },
            { "spry", "SPRAY" },
            { "spray", "SPRAY" },
            { "spr", "SPRAY" },
            { "film", "FILM" },
            { "gel", "GEL" },
            { "lot", "LOTION" },
            { "lotion", "LOTION" },
            { "aerosol", "SPRAY" },
            { "inhalant", "INHALANT" },
            { "inhaler", "INHALANT" },
            { "hfa", "INHALANT" },
            { "liq", "SOLUTION" },
            { "liquid", "SOLUTION" }
        };

        /// <summary>
        /// Parses a raw drug name into clinical components (active ingredients, strengths, dosage form, ER flag).
        /// Eliminates "/" and "\" delimiters per Medicine Master specification.
        /// </summary>
        public static ParsedDrugComponents ParseDrug(string? rawDrug)
        {
            if (string.IsNullOrWhiteSpace(rawDrug))
                return new ParsedDrugComponents(new HashSet<string>(), new List<decimal>(), null, false, string.Empty);

            // 1. User requirement: Eliminate '/' and '\' from DrugName/Brand
            var cleaned = rawDrug.Replace('\\', ' ').Replace('/', ' ');
            cleaned = Regex.Replace(cleaned, @"-([a-zA-Z]{4})\b", " "); // Strip FDA 4-letter biosimilar suffixes (e.g. -yfgn, -aglr)
            cleaned = Regex.Replace(cleaned, @"(?<!\d)-(?!\d)", " "); // Replace dashes with spaces unless between numbers (e.g. 10-325)
            cleaned = Regex.Replace(cleaned, @"[,;:()\[\]]", " ");
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

            // 2. Detect Extended Release
            var isER = Regex.IsMatch(cleaned, @"\b(ER|XR|XL|CR|SR|EXTENDED RELEASE|24 HOUR|12 HOUR)\b", RegexOptions.IgnoreCase);

            // 3. Extract Strengths (e.g. 10-325, 10-325MG, 0.5MG, 25MG, 100U/ML, 50000 UT)
            var strengths = new List<decimal>();
            var multiStrengthMatch = Regex.Match(cleaned, @"\b(\d+(?:\.\d+)?)-(\d+(?:\.\d+)?)\s*(?:MG|MCG|G|ML)?\b", RegexOptions.IgnoreCase);
            if (multiStrengthMatch.Success)
            {
                if (decimal.TryParse(multiStrengthMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var s1))
                    strengths.Add(s1);
                if (decimal.TryParse(multiStrengthMatch.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var s2))
                    strengths.Add(s2);
            }
            else
            {
                var singleMatches = Regex.Matches(cleaned, @"\b(\d+(?:\.\d+)?)\s*(?:MG|MCG|G|ML|%|U(?:NIT(?:S)?)?(?:/ML)?|IU(?:/ML)?|UT)\b", RegexOptions.IgnoreCase);
                foreach (Match m in singleMatches)
                {
                    if (decimal.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
                        strengths.Add(s);
                }

                if (strengths.Count == 0)
                {
                    var numMatches = Regex.Matches(cleaned, @"\b(\d+(?:\.\d+)?)\b");
                    foreach (Match m in numMatches)
                    {
                        if (decimal.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var s) && s > 0 && s < 500000)
                        {
                            strengths.Add(s);
                        }
                    }
                }
            }
            strengths.Sort();

            // 4. Extract Dosage Form
            string? dosageForm = null;
            var tokens = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tokens)
            {
                var lower = t.ToLowerInvariant();
                if (DosageFormMap.TryGetValue(lower, out var mappedForm))
                {
                    dosageForm = mappedForm;
                    break;
                }
            }

            // 5. Extract Ingredients
            var ingredients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ignoreWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "oral", "tablet", "tablets", "tab", "tabs", "capsule", "capsules", "cap", "caps",
                "solution", "sol", "soln", "suspension", "susp", "mg", "mcg", "g", "ml",
                "er", "xr", "xl", "cr", "sr", "extended", "release", "hour", "hr", "and", "with",
                "bitartrate", "hydrochloride", "hcl", "sodium", "potassium", "calcium", "maleate",
                "besylate", "fumarate", "succinate", "tartrate", "mesylate", "gluconate", "hydrate",
                "acetate", "citrate", "phosphate", "sulfate", "propionate", "dipropionate", "valerate",
                "palmitate", "monohydrate", "dihydrate", "trihydrate", "hydrobromide", "hbr", "nitrate",
                "bromide", "chloride", "carbonate",
                // Devices, delivery mechanisms & packaging modifiers
                "solostar", "flexpen", "kwikpen", "flextouch", "innolet", "diskus", "respimat", "ellipta",
                "pen", "injector", "peninjector", "autoinjector", "inhaler", "inhalant",
                "unit", "units", "international", "subcutaneous", "subq", "inj", "injection"
            };

            foreach (var t in tokens)
            {
                var cleanToken = Regex.Replace(t, @"[^\w]", "");
                if (cleanToken.Length < 2) continue;
                if (char.IsDigit(cleanToken[0])) continue;
                if (ignoreWords.Contains(cleanToken)) continue;

                // Expand abbreviation
                if (StandardAbbreviations.TryGetValue(cleanToken, out var expanded))
                {
                    ingredients.Add(expanded.ToLowerInvariant());
                    continue;
                }

                // Check Brand map
                if (BrandToGenericMap.TryGetValue(cleanToken, out var genericMapped))
                {
                    var parts = genericMapped.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts)
                    {
                        ingredients.Add(p.Trim().ToLowerInvariant());
                    }
                    continue;
                }

                ingredients.Add(cleanToken.ToLowerInvariant());
            }

            return new ParsedDrugComponents(ingredients, strengths, dosageForm, isER, cleaned);
        }

        /// <summary>
        /// Evaluates clinical equivalence between two drug names (e.g. MicroMerchant vs ERX),
        /// resolving brand vs generic names, strength normalization, dosage form aliases,
        /// and Medicine Master records.
        /// </summary>
        public static bool AreDrugsClinicallyEquivalent(
            string? mmDrug,
            string? erxDrug,
            string? mmPotencyUnit = null,
            string? erxPotencyUnit = null,
            RxintelDbContext? dbContext = null)
        {
            if (string.IsNullOrWhiteSpace(mmDrug) && string.IsNullOrWhiteSpace(erxDrug))
                return true;
            if (string.IsNullOrWhiteSpace(mmDrug) || string.IsNullOrWhiteSpace(erxDrug))
                return false;

            // Eliminate '/' and '\' per Medicine Master rules
            var mmClean = mmDrug.Replace('\\', ' ').Replace('/', ' ').Trim();
            var erxClean = erxDrug.Replace('\\', ' ').Replace('/', ' ').Trim();

            if (string.Equals(mmClean, erxClean, StringComparison.OrdinalIgnoreCase))
                return true;

            var p1 = ParseDrug(mmDrug);
            var p2 = ParseDrug(erxDrug);

            // 1. Compare Strengths
            if (p1.Strengths.Count > 0 && p2.Strengths.Count > 0)
            {
                if (p1.Strengths.Count == p2.Strengths.Count)
                {
                    for (int i = 0; i < p1.Strengths.Count; i++)
                    {
                        if (Math.Abs(p1.Strengths[i] - p2.Strengths[i]) > 0.001m)
                            return false; // Strengths differ!
                    }
                }
                else
                {
                    // Multi-ingredient strength check (e.g. [10, 325])
                    var set1 = new HashSet<decimal>(p1.Strengths);
                    var set2 = new HashSet<decimal>(p2.Strengths);
                    if (!set1.SetEquals(set2))
                    {
                        // Allow if one is subset
                        if (!set1.IsSubsetOf(set2) && !set2.IsSubsetOf(set1))
                            return false;
                    }
                }
            }

            // 2. Compare Dosage Form (incorporating PotencyUnitCode if available)
            var form1 = p1.DosageForm ?? NormalizeForm(mmPotencyUnit);
            var form2 = p2.DosageForm ?? NormalizeForm(erxPotencyUnit);

            if (!string.IsNullOrEmpty(form1) && !string.IsNullOrEmpty(form2))
            {
                if (!string.Equals(form1, form2, StringComparison.OrdinalIgnoreCase))
                {
                    // Allow Tablet vs Capsule only if both are solid oral forms and ingredients match
                    var isSolidOral1 = form1 is "TABLET" or "CAPSULE";
                    var isSolidOral2 = form2 is "TABLET" or "CAPSULE";
                    if (!isSolidOral1 || !isSolidOral2)
                        return false;
                }
            }

            // 3. Compare Ingredients
            if (p1.Ingredients.Count > 0 && p2.Ingredients.Count > 0)
            {
                var matchAllP1InP2 = p1.Ingredients.All(ing1 => p2.Ingredients.Any(ing2 => IngredientsMatch(ing1, ing2, dbContext)));
                var matchAllP2InP1 = p2.Ingredients.All(ing2 => p1.Ingredients.Any(ing1 => IngredientsMatch(ing1, ing2, dbContext)));

                if (matchAllP1InP2 && matchAllP2InP1)
                    return true;

                // Check if one drug is Brand of the other via DB lookup
                if (dbContext != null && CheckMedicineMasterEquivalence(p1, p2, dbContext))
                    return true;
            }

            // Fallback: Check MedicineMaster table directly
            if (dbContext != null && CheckMedicineMasterEquivalence(p1, p2, dbContext))
                return true;

            return false;
        }

        /// <summary>
        /// Evaluates NDC equivalence.
        /// Checks if both NDCs are present in Medicine Master and represent the same medicine.
        /// Also checks FDA 9-digit ProductNDC equivalence and clinical drug equivalence.
        /// </summary>
        public static bool AreNdcsEquivalent(
            string? mmNdc,
            string? erxNdc,
            ErxAuditRecord? record,
            RxintelDbContext? dbContext = null)
        {
            if (string.IsNullOrWhiteSpace(mmNdc) && string.IsNullOrWhiteSpace(erxNdc))
                return true;
            if (string.IsNullOrWhiteSpace(mmNdc) || string.IsNullOrWhiteSpace(erxNdc))
                return false;

            var cleanMm = Regex.Replace(mmNdc.Trim(), @"[^\w]", "");
            var cleanErx = Regex.Replace(erxNdc.Trim(), @"[^\w]", "");

            // 1. Exact or normalized string match
            if (string.Equals(cleanMm, cleanErx, StringComparison.OrdinalIgnoreCase))
                return true;

            // Trim leading zeros for normalized comparison (e.g. 00406000000 vs 406000000)
            if (string.Equals(cleanMm.TrimStart('0'), cleanErx.TrimStart('0'), StringComparison.OrdinalIgnoreCase))
                return true;

            // 2. FDA 10-to-11 digit NDC conversion
            if (cleanMm.Length == 10 && cleanErx.Length == 11 && Is10To11NdcMatch(cleanMm, cleanErx))
                return true;
            if (cleanMm.Length == 11 && cleanErx.Length == 10 && Is10To11NdcMatch(cleanErx, cleanMm))
                return true;

            // 3. FDA ProductNDC (first 8-9 digits) match:
            // Identical labeler and product code from same manufacturer differing only in packaging configuration
            if (cleanMm.Length >= 9 && cleanErx.Length >= 9 &&
                string.Equals(cleanMm.Substring(0, 9), cleanErx.Substring(0, 9), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 4. Medicine Master Check:
            // Check if both NDCs are present in Medicine Master and represent the same medicine.
            if (dbContext != null)
            {
                var med1 = FindMedicineInMaster(cleanMm, dbContext);
                var med2 = FindMedicineInMaster(cleanErx, dbContext);

                if (med1 != null && med2 != null)
                {
                    if (AreMedicineMasterEntriesEquivalent(med1, med2, dbContext, record))
                    {
                        return true;
                    }
                }

                // If one NDC exists in Medicine Master and its ProductNDC matches the other's prefix
                if (med1 != null && !string.IsNullOrWhiteSpace(med1.ProductNDC) && cleanErx.StartsWith(med1.ProductNDC, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (med2 != null && !string.IsNullOrWhiteSpace(med2.ProductNDC) && cleanMm.StartsWith(med2.ProductNDC, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // 5. Clinical equivalence check from prescription record
            if (record != null)
            {
                var isDrugMatch = AreDrugsClinicallyEquivalent(
                    record.MmDispensedDrugName,
                    record.ErxDrugName,
                    record.MmPotencyUnitCode,
                    record.ErxPotencyUnitCode,
                    dbContext);

                if (isDrugMatch)
                {
                    var p1 = ParseDrug(record.MmDispensedDrugName);
                    var p2 = ParseDrug(record.ErxDrugName);

                    // Strength check
                    bool sameStrengths = (p1.Strengths.Count == 0 && p2.Strengths.Count == 0) ||
                                         (p1.Strengths.Count > 0 && p2.Strengths.Count > 0 && p1.Strengths.SequenceEqual(p2.Strengths)) ||
                                         (p1.Strengths.Count == 0 || p2.Strengths.Count == 0);

                    // Check percentage strength (e.g. 5% patch)
                    if (!sameStrengths)
                    {
                        var m1 = Regex.Match(record.MmDispensedDrugName ?? "", @"\b(\d+(?:\.\d+)?)\s*%\b");
                        var m2 = Regex.Match(record.ErxDrugName ?? "", @"\b(\d+(?:\.\d+)?)\s*%\b");
                        if (m1.Success && m2.Success && m1.Groups[1].Value == m2.Groups[1].Value)
                            sameStrengths = true;
                    }

                    // Dosage Form check
                    var form1 = p1.DosageForm ?? NormalizeForm(record.MmPotencyUnitCode);
                    var form2 = p2.DosageForm ?? NormalizeForm(record.ErxPotencyUnitCode);
                    bool sameForm = AreDosageFormsCompatible(form1, form2);

                    if (sameStrengths && sameForm)
                    {
                        return true;
                    }
                }
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

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Medicine?> MasterCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Finds a Medicine Master entry by NDC using exact, clean, unzero, and 10/11 digit permutations.
        /// Results (both matches and nulls) are cached in memory for sub-millisecond execution.
        /// </summary>
        public static Medicine? FindMedicineInMaster(string? ndc, RxintelDbContext? dbContext)
        {
            if (string.IsNullOrWhiteSpace(ndc) || dbContext == null) return null;
            var clean = Regex.Replace(ndc.Trim(), @"[^\w]", "");
            if (string.IsNullOrEmpty(clean)) return null;

            if (MasterCache.TryGetValue(clean, out var cached))
                return cached;

            if (string.Equals(clean, "SUPPLY", StringComparison.OrdinalIgnoreCase))
            {
                MasterCache[clean] = null;
                return null;
            }

            try
            {
                // 1. Direct match on NDC or NormalizedNdc
                var med = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(dbContext.MedicineMasters)
                    .FirstOrDefault(m => m.NDC == ndc || m.NDC == clean || m.NormalizedNdc == clean);
                if (med != null)
                {
                    MasterCache[clean] = med;
                    return med;
                }

                // 2. Unpadded match (trim leading zeros)
                var unzero = clean.TrimStart('0');
                if (!string.IsNullOrEmpty(unzero) && unzero != clean)
                {
                    med = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(dbContext.MedicineMasters)
                        .FirstOrDefault(m => m.NormalizedNdc == unzero || m.NDC == unzero);
                    if (med != null)
                    {
                        MasterCache[clean] = med;
                        return med;
                    }
                }

                // 3. Match by ProductNDC prefix (e.g. 9-digit match)
                if (clean.Length >= 9)
                {
                    var prodPrefix = clean.Substring(0, 9);
                    med = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(dbContext.MedicineMasters)
                        .FirstOrDefault(m => m.ProductNDC == prodPrefix);
                    if (med != null)
                    {
                        MasterCache[clean] = med;
                        return med;
                    }
                }

                // 4. 10-digit to 11-digit or vice versa
                if (clean.Length == 10)
                {
                    var p1 = "0" + clean;
                    var p2 = clean.Substring(0, 5) + "0" + clean.Substring(5);
                    var p3 = clean.Substring(0, 9) + "0" + clean.Substring(9);
                    med = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(dbContext.MedicineMasters)
                        .FirstOrDefault(m => m.NormalizedNdc == p1 || m.NormalizedNdc == p2 || m.NormalizedNdc == p3 ||
                                             m.NDC == p1 || m.NDC == p2 || m.NDC == p3);
                    if (med != null)
                    {
                        MasterCache[clean] = med;
                        return med;
                    }
                }
                else if (clean.Length == 11 && clean.StartsWith('0'))
                {
                    var ten = clean.Substring(1);
                    med = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(dbContext.MedicineMasters)
                        .FirstOrDefault(m => m.NormalizedNdc == ten || m.NDC == ten);
                    if (med != null)
                    {
                        MasterCache[clean] = med;
                        return med;
                    }
                }
            }
            catch
            {
                // Graceful fallback
            }

            MasterCache[clean] = null;
            return null;
        }

        /// <summary>
        /// Compares two Medicine Master records to determine if they represent the same clinical drug product.
        /// </summary>
        public static bool AreMedicineMasterEntriesEquivalent(
            Medicine med1,
            Medicine med2,
            RxintelDbContext? dbContext = null,
            ErxAuditRecord? record = null)
        {
            if (med1 == null || med2 == null) return false;
            if (med1.Id == med2.Id) return true;

            // 1. Same FDA ProductNDC
            if (!string.IsNullOrWhiteSpace(med1.ProductNDC) && !string.IsNullOrWhiteSpace(med2.ProductNDC) &&
                string.Equals(med1.ProductNDC, med2.ProductNDC, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 2. Check NDCGroup linking
            if (dbContext != null)
            {
                try
                {
                    var isLinkedByNdcGroup = dbContext.NDCGroups.Any(g =>
                        (g.ParentMedicineId == med1.Id && g.ChildMedicineId == med2.Id) ||
                        (g.ParentMedicineId == med2.Id && g.ChildMedicineId == med1.Id));
                    if (isLinkedByNdcGroup) return true;
                }
                catch { }
            }

            // 3. Check Dosage Form compatibility
            if (!AreDosageFormsCompatible(med1.DosageForm, med2.DosageForm))
            {
                return false;
            }

            // 4. Exact Clean Brand or Generic Match (e.g. FLUTICASONE PROPIONATE == Fluticasone Propionate)
            // Cross-match: Brand1 == Brand2, Generic1 == Generic2, Brand1 == Generic2, Brand2 == Generic1, or contains
            var cleanBrand1 = CleanDrugString(med1.Brand);
            var cleanBrand2 = CleanDrugString(med2.Brand);
            var cleanGen1 = CleanDrugString(med1.Generic);
            var cleanGen2 = CleanDrugString(med2.Generic);

            var brandMatches = !string.IsNullOrEmpty(cleanBrand1) && cleanBrand1 == cleanBrand2;
            var genericMatches = !string.IsNullOrEmpty(cleanGen1) && cleanGen1 == cleanGen2;
            var crossBrandGenMatches = (!string.IsNullOrEmpty(cleanBrand1) && !string.IsNullOrEmpty(cleanGen2) && (cleanBrand1 == cleanGen2 || cleanGen2.Contains(cleanBrand1) || cleanBrand1.Contains(cleanGen2))) ||
                                       (!string.IsNullOrEmpty(cleanBrand2) && !string.IsNullOrEmpty(cleanGen1) && (cleanBrand2 == cleanGen1 || cleanGen1.Contains(cleanBrand2) || cleanBrand2.Contains(cleanGen1)));

            // 5. Active Ingredients equivalence
            var ing1 = ParseDrug(string.Concat(med1.Brand, " ", med1.Generic)).Ingredients;
            var ing2 = ParseDrug(string.Concat(med2.Brand, " ", med2.Generic)).Ingredients;

            bool ingredientsMatch = false;
            if (ing1.Count > 0 && ing2.Count > 0)
            {
                if (ing1.SetEquals(ing2))
                {
                    ingredientsMatch = true;
                }
                else
                {
                    bool all1In2 = ing1.All(i1 => ing2.Any(i2 => IngredientsMatch(i1, i2, dbContext)));
                    bool all2In1 = ing2.All(i2 => ing1.Any(i1 => IngredientsMatch(i1, i2, dbContext)));
                    if (all1In2 || all2In1)
                    {
                        ingredientsMatch = true;
                    }
                }
            }

            if (!brandMatches && !genericMatches && !crossBrandGenMatches && !ingredientsMatch)
            {
                return false;
            }

            // 6. Strengths Compatibility check
            if (AreStrengthsCompatible(med1, med2, record))
            {
                return true;
            }

            return false;
        }

        public static bool AreDosageFormsCompatible(string? raw1, string? raw2)
        {
            if (string.IsNullOrWhiteSpace(raw1) || string.IsNullOrWhiteSpace(raw2))
                return true;

            var f1 = NormalizeForm(raw1) ?? raw1.Trim().ToUpperInvariant();
            var f2 = NormalizeForm(raw2) ?? raw2.Trim().ToUpperInvariant();

            if (string.Equals(f1, f2, StringComparison.OrdinalIgnoreCase))
                return true;

            // Substring contains check (e.g. "SPRAY" inside "SPRAY, METERED", "TABLET" inside "TABLET, EXTENDED RELEASE")
            if (f1.Contains(f2, StringComparison.OrdinalIgnoreCase) || f2.Contains(f1, StringComparison.OrdinalIgnoreCase))
                return true;

            if (IsSolidOralForm(f1) && IsSolidOralForm(f2)) return true;
            if (IsPatchOrFilmForm(f1) && IsPatchOrFilmForm(f2)) return true;
            if (IsSprayForm(f1) && IsSprayForm(f2)) return true;
            if (IsLiquidForm(f1) && IsLiquidForm(f2)) return true;
            if (IsInjectionForm(f1) && IsInjectionForm(f2)) return true;
            if ((IsInjectionForm(f1) || IsLiquidForm(f1)) && (IsInjectionForm(f2) || IsLiquidForm(f2)) && (IsInjectionForm(f1) || IsInjectionForm(f2))) return true;

            return false;
        }

        private static bool IsSolidOralForm(string form) =>
            form.Contains("TABLET", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("CAPSULE", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("TAB", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("CAP", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("PELLET", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("LOZENGE", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("TROCHE", StringComparison.OrdinalIgnoreCase);

        private static bool IsPatchOrFilmForm(string form) =>
            form.Contains("PATCH", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("FILM", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("TRANSDERMAL", StringComparison.OrdinalIgnoreCase);

        private static bool IsSprayForm(string form) =>
            form.Contains("SPRAY", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("SPRY", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("AEROSOL", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("INHALANT", StringComparison.OrdinalIgnoreCase);

        private static bool IsLiquidForm(string form) =>
            form.Contains("SOLUTION", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("SUSPENSION", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("LIQUID", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("SYRUP", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("ELIXIR", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("DROPS", StringComparison.OrdinalIgnoreCase);

        private static bool IsInjectionForm(string form) =>
            form.Contains("INJECTION", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("INJECTABLE", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("INJ", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("PEN-INJECTOR", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("PEN INJECTOR", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("AUTO-INJECTOR", StringComparison.OrdinalIgnoreCase) ||
            form.Contains("AUTOINJECTOR", StringComparison.OrdinalIgnoreCase);

        public static bool AreStrengthsCompatible(Medicine med1, Medicine med2, ErxAuditRecord? record = null)
        {
            var s1 = ExtractStrengthsFromMedicine(med1);
            var s2 = ExtractStrengthsFromMedicine(med2);

            if (s1.Count == 0 || s2.Count == 0)
                return true;

            if (s1.SequenceEqual(s2))
                return true;

            // Lidocaine patch clinical equivalence (50 mg/g [5%] vs 140 mg deliverable dose per 5% patch)
            var isLido1 = (med1.Brand?.Contains("lidocaine", StringComparison.OrdinalIgnoreCase) == true) ||
                          (med1.Generic?.Contains("lidocaine", StringComparison.OrdinalIgnoreCase) == true);
            var isLido2 = (med2.Brand?.Contains("lidocaine", StringComparison.OrdinalIgnoreCase) == true) ||
                          (med2.Generic?.Contains("lidocaine", StringComparison.OrdinalIgnoreCase) == true);
            if (isLido1 && isLido2)
            {
                bool validLido1 = s1.Contains(5) || s1.Contains(50) || s1.Contains(140) || med1.Brand?.Contains("5%") == true;
                bool validLido2 = s2.Contains(5) || s2.Contains(50) || s2.Contains(140) || med2.Brand?.Contains("5%") == true;
                if (validLido1 && validLido2)
                    return true;
            }

            // Check if prescription record explicitly confirms matching strength
            if (record != null)
            {
                var p1 = ParseDrug(record.MmDispensedDrugName);
                var p2 = ParseDrug(record.ErxDrugName);
                if (p1.Strengths.Count > 0 && p2.Strengths.Count > 0 && p1.Strengths.SequenceEqual(p2.Strengths))
                    return true;

                var m1 = Regex.Match(record.MmDispensedDrugName ?? "", @"\b(\d+(?:\.\d+)?)\s*%\b");
                var m2 = Regex.Match(record.ErxDrugName ?? "", @"\b(\d+(?:\.\d+)?)\s*%\b");
                if (m1.Success && m2.Success && m1.Groups[1].Value == m2.Groups[1].Value)
                    return true;
            }

            // Check percentage strengths in Brand/Generic text
            var pct1 = Regex.Match(string.Concat(med1.Brand, " ", med1.Generic), @"\b(\d+(?:\.\d+)?)\s*%\b");
            var pct2 = Regex.Match(string.Concat(med2.Brand, " ", med2.Generic), @"\b(\d+(?:\.\d+)?)\s*%\b");
            if (pct1.Success && pct2.Success && pct1.Groups[1].Value == pct2.Groups[1].Value)
                return true;

            return false;
        }

        private static List<decimal> ExtractStrengthsFromMedicine(Medicine med)
        {
            var list = new List<decimal>();
            if (!string.IsNullOrWhiteSpace(med.Strength))
            {
                var matches = Regex.Matches(med.Strength, @"\b(\d+(?:\.\d+)?)\b");
                foreach (Match m in matches)
                {
                    if (decimal.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
                        list.Add(s);
                }
            }
            list.Sort();
            return list;
        }

        private static string CleanDrugString(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var s = text.Replace('\\', ' ').Replace('/', ' ').Replace(',', ' ');
            s = Regex.Replace(s, @"-([a-zA-Z]{4})\b", " "); // Strip FDA 4-letter biosimilar suffixes (-yfgn, -aglr)
            s = Regex.Replace(s, @"\b(solostar|flexpen|kwikpen|flextouch|innolet|diskus|respimat|ellipta|pen|injector|pen-injector|peninjector|autoinjector|inhaler|inhalant|subcutaneous|subq|inj|injection)\b", " ", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\b(and|with|tab|tabs|tablet|tablets|cap|caps|capsule|capsules|besylate|hcl|hydrochloride)\b", " ", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"[^\w\s]", " ");
            return Regex.Replace(s, @"\s+", " ").Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Queries the Medicine Master table by drug name (sanitizing "/" and "\"),
        /// dosage form, strength, and package size.
        /// </summary>
        public static MedicineMasterLookupResult? LookupMedicineMaster(
            string? drugName,
            string? dosageForm = null,
            string? strength = null,
            string? packageSize = null,
            RxintelDbContext? dbContext = null)
        {
            if (string.IsNullOrWhiteSpace(drugName) || dbContext == null)
                return null;

            try
            {
                var parsed = ParseDrug(drugName);
                var firstWord = parsed.Ingredients.FirstOrDefault();
                if (string.IsNullOrEmpty(firstWord))
                    return null;

                var query = dbContext.MedicineMasters
                    .Where(m => m.Brand.Contains(firstWord) || (m.Generic != null && m.Generic.Contains(firstWord)));

                // If multiple ingredients, check second
                var ingredientsList = parsed.Ingredients.ToList();
                if (ingredientsList.Count > 1)
                {
                    var secondWord = ingredientsList[1];
                    query = query.Where(m => m.Brand.Contains(secondWord) || (m.Generic != null && m.Generic.Contains(secondWord)));
                }

                var matches = query.Take(20).ToList();
                if (matches.Count == 0) return null;

                // Filter by dosage form if provided
                var targetForm = parsed.DosageForm ?? NormalizeForm(dosageForm);
                if (!string.IsNullOrEmpty(targetForm))
                {
                    var formFiltered = matches.Where(m => m.DosageForm != null && m.DosageForm.Contains(targetForm, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (formFiltered.Count > 0)
                        matches = formFiltered;
                }

                // Filter by package size if provided
                if (!string.IsNullOrWhiteSpace(packageSize))
                {
                    var pkgFiltered = matches.Where(m => string.Equals(m.PackageSize, packageSize.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                    if (pkgFiltered.Count > 0)
                        matches = pkgFiltered;
                }

                var best = matches.FirstOrDefault();
                if (best == null) return null;

                return new MedicineMasterLookupResult
                {
                    IsMatched = true,
                    DrugName = best.Brand,
                    GenericName = best.Generic,
                    DosageForm = best.DosageForm,
                    Strength = best.Strength,
                    StrengthUnit = best.StrengthUnit,
                    PackageSize = best.PackageSize,
                    Ndc = best.NDC,
                    MatchReason = "Matched via Medicine Master component lookup"
                };
            }
            catch
            {
                return null;
            }
        }

        private static string? NormalizeForm(string? unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return null;
            var clean = unit.Trim().ToLowerInvariant();
            return DosageFormMap.TryGetValue(clean, out var form) ? form : clean.ToUpperInvariant();
        }

        public static bool AreSuppliesClinicallyEquivalent(string? d1, string? d2)
        {
            if (string.IsNullOrWhiteSpace(d1) || string.IsNullOrWhiteSpace(d2)) return false;
            var isLancet1 = Regex.IsMatch(d1, @"\blancets?\b", RegexOptions.IgnoreCase);
            var isLancet2 = Regex.IsMatch(d2, @"\blancets?\b", RegexOptions.IgnoreCase);
            if (isLancet1 && isLancet2) return true;

            var isStrip1 = Regex.IsMatch(d1, @"\b(test\s*strips?|strips?)\b", RegexOptions.IgnoreCase);
            var isStrip2 = Regex.IsMatch(d2, @"\b(test\s*strips?|strips?)\b", RegexOptions.IgnoreCase);
            if (isStrip1 && isStrip2) return true;

            var isNeedle1 = Regex.IsMatch(d1, @"\b(pen\s*ndl|needles?)\b", RegexOptions.IgnoreCase);
            var isNeedle2 = Regex.IsMatch(d2, @"\b(pen\s*ndl|needles?)\b", RegexOptions.IgnoreCase);
            if (isNeedle1 && isNeedle2) return true;

            return false;
        }

        private static bool IngredientsMatch(string ing1, string ing2, RxintelDbContext? dbContext)
        {
            if (string.Equals(ing1, ing2, StringComparison.OrdinalIgnoreCase))
                return true;

            if (ing1.Contains(ing2, StringComparison.OrdinalIgnoreCase) || ing2.Contains(ing1, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check BrandToGenericMap
            if (BrandToGenericMap.TryGetValue(ing1, out var g1) && (g1.Contains(ing2, StringComparison.OrdinalIgnoreCase) || string.Equals(g1, ing2, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (BrandToGenericMap.TryGetValue(ing2, out var g2) && (g2.Contains(ing1, StringComparison.OrdinalIgnoreCase) || string.Equals(g2, ing1, StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        private static bool CheckMedicineMasterEquivalence(ParsedDrugComponents p1, ParsedDrugComponents p2, RxintelDbContext dbContext)
        {
            // Fast in-memory evaluation avoids full table scans on remote SQL Server
            var text1 = string.Join(" ", p1.Ingredients);
            var text2 = string.Join(" ", p2.Ingredients);
            return AreSuppliesClinicallyEquivalent(text1, text2);
        }
    }
}
