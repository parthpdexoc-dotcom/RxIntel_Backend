using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Rxintel.Generic.Services.MetaAI
{
    /// <summary>
    /// Represents a persistent learned clinical equivalence pair evaluated by Muse AI or approved by an auditor.
    /// </summary>
    public class LearnedSigEntry
    {
        public string MmSig { get; set; } = string.Empty;
        public string ErxSig { get; set; } = string.Empty;
        public bool IsCompliant { get; set; }
        public double Confidence { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Source { get; set; } = "Muse AI";
        public DateTime LearnedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Autonomous Memory &amp; Continuous Learning Store for Muse AI.
    /// Eliminates the need for manual local SIG configuration by remembering all verified clinical equivalences.
    /// Backed by a thread-safe in-memory cache and persisted to ReferenceData/ai_sig_memory.json.
    /// </summary>
    public static class AiSigKnowledgeMemory
    {
        private static readonly ConcurrentDictionary<string, LearnedSigEntry> _memory =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly object _fileLock = new();
        private static string? _storageFilePath;
        private static bool _initialized;

        /// <summary>
        /// Initializes the continuous learning memory store, loading existing learned equivalences from disk.
        /// </summary>
        public static void Initialize(string? referenceDataFolder = null)
        {
            if (_initialized) return;

            try
            {
                var folder = referenceDataFolder;
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferenceData");
                }
                Directory.CreateDirectory(folder);

                _storageFilePath = Path.Combine(folder, "ai_sig_memory.json");

                // Seed baseline clinical equivalences (including MDI inhaler phrasing)
                SeedBaselineMemory();

                if (File.Exists(_storageFilePath))
                {
                    var json = File.ReadAllText(_storageFilePath);
                    var list = JsonSerializer.Deserialize<List<LearnedSigEntry>>(json);
                    if (list != null)
                    {
                        foreach (var entry in list)
                        {
                            var k1 = GenerateKey(entry.MmSig, entry.ErxSig);
                            var k2 = GenerateKey(entry.ErxSig, entry.MmSig);
                            _memory[k1] = entry;
                            _memory[k2] = entry;
                        }
                    }
                }

                _initialized = true;
            }
            catch
            {
                _initialized = true;
            }
        }

        /// <summary>
        /// Checks if a SIG pair is already known in Muse's learned memory.
        /// </summary>
        public static bool TryGetLearnedEquivalence(string? mmSig, string? erxSig, out MetaAiEvaluationResult? result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(mmSig) || string.IsNullOrWhiteSpace(erxSig))
                return false;

            var key = GenerateKey(mmSig, erxSig);
            if (_memory.TryGetValue(key, out var entry))
            {
                result = new MetaAiEvaluationResult
                {
                    IsCompliant = entry.IsCompliant,
                    Confidence = entry.Confidence,
                    Model = "muse-memory",
                    Evaluator = $"Muse Learned Memory ({entry.Source})",
                    Reason = entry.Reason,
                    LatencyMs = 0,
                    FromCache = true
                };
                return true;
            }

            return false;
        }

        /// <summary>
        /// Records a verified clinical equivalence into Muse's continuous memory and persists it to disk.
        /// </summary>
        public static void RecordLearnedEquivalence(
            string mmSig,
            string erxSig,
            bool isCompliant,
            double confidence,
            string reason,
            string source = "Muse AI")
        {
            if (string.IsNullOrWhiteSpace(mmSig) || string.IsNullOrWhiteSpace(erxSig))
                return;

            var entry = new LearnedSigEntry
            {
                MmSig = mmSig.Trim(),
                ErxSig = erxSig.Trim(),
                IsCompliant = isCompliant,
                Confidence = confidence,
                Reason = reason,
                Source = source,
                LearnedAt = DateTime.UtcNow
            };

            var k1 = GenerateKey(mmSig, erxSig);
            var k2 = GenerateKey(erxSig, mmSig);
            _memory[k1] = entry;
            _memory[k2] = entry;

            ThreadPool.QueueUserWorkItem(_ => PersistToFile());
        }

        private static string GenerateKey(string sig1, string sig2)
        {
            var s1 = CleanString(sig1);
            var s2 = CleanString(sig2);
            return $"{s1}|||{s2}";
        }

        private static string CleanString(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return string.Join(" ", text.Trim().ToUpperInvariant()
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private static void SeedBaselineMemory()
        {
            // Seed 1: The exact Inhaler/Actuation MDI case from clinical audit
            RecordLearnedEquivalence(
                "USE 2 ACTUATIONS BID VIA ORAL INHALATION FOR SYMPTOMS",
                "Inhale 2 puffs into the lungs every morning and evening for wheezing prevention",
                isCompliant: true,
                confidence: 1.0,
                reason: "Clinically equivalent: 2 actuations BID via oral inhalation is identical to 2 puffs into the lungs morning and evening (4 puffs/day total dose). Indication phrasing (for symptoms vs wheezing prevention) does not alter dosing regimen.",
                source: "Clinical Baseline Seed"
            );

            // Seed 2: Standard Latin abbreviation expansions
            RecordLearnedEquivalence(
                "TAKE 1 TABLET BY MOUTH TWICE DAILY WITH MEALS",
                "1 TAB PO BID WF",
                isCompliant: true,
                confidence: 1.0,
                reason: "Exact clinical equivalent: PO = by mouth, BID = twice daily, WF = with food.",
                source: "Clinical Baseline Seed"
            );

            // Seed 3: Topical AAA abbreviation
            RecordLearnedEquivalence(
                "APPLY TO AFFECTED AREA TWICE DAILY",
                "AAA BID",
                isCompliant: true,
                confidence: 1.0,
                reason: "Exact clinical equivalent: AAA = apply to affected area, BID = twice daily.",
                source: "Clinical Baseline Seed"
            );
        }

        private static void PersistToFile()
        {
            if (string.IsNullOrWhiteSpace(_storageFilePath)) return;

            try
            {
                lock (_fileLock)
                {
                    var distinctEntries = _memory.Values
                        .GroupBy(x => GenerateKey(x.MmSig, x.ErxSig))
                        .Select(g => g.First())
                        .ToList();

                    var json = JsonSerializer.Serialize(distinctEntries, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_storageFilePath, json);
                }
            }
            catch
            {
                // Non-fatal IO log
            }
        }
    }
}
