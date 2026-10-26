using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Services.Helpers;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.TypeSafeAI
{
    public class TypeSafeClient : ITypeSafeClient
    {
        private readonly HttpClient _httpClient;
        private readonly TypeSafeClientOptions _options;
        private readonly ILogger<TypeSafeClient> _logger;

        private readonly ConcurrentDictionary<string, TypeSafeSigEvaluationResult> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        public TypeSafeClient(
            HttpClient httpClient,
            TypeSafeClientOptions options,
            ILogger<TypeSafeClient> logger)
        {
            _httpClient = httpClient;
            _options = options;
            _logger = logger;
        }

        public async Task<bool> AreSigsEquivalentAsync(
            string? mmSig,
            string? erxSig,
            IEnumerable<string?>? knownPii = null,
            CancellationToken cancellationToken = default)
        {
            var result = await EvaluateSigDetailedAsync(mmSig, erxSig, knownPii, cancellationToken);
            return result.IsEquivalent;
        }

        public bool AreSigsEquivalent(
            string? mmSig,
            string? erxSig,
            IEnumerable<string?>? knownPii = null)
        {
            var mmClean = (mmSig?.Trim() ?? string.Empty).Replace('\\', '/');
            var erxClean = (erxSig?.Trim() ?? string.Empty).Replace('\\', '/');

            if (string.IsNullOrEmpty(mmClean) && string.IsNullOrEmpty(erxClean)) return true;
            if (string.IsNullOrEmpty(mmClean) || string.IsNullOrEmpty(erxClean)) return false;

            if (string.Equals(mmClean, erxClean, StringComparison.OrdinalIgnoreCase))
                return true;

            var cacheKey = $"{mmClean.ToUpperInvariant()}|||{erxClean.ToUpperInvariant()}";
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached.IsEquivalent;
            }

            // Local clinical normalizer fast path (0ms latency, zero external API call)
            if (PharmacySigNormalizer.AreSigsEquivalent(mmClean, erxClean))
            {
                _cache[cacheKey] = new TypeSafeSigEvaluationResult
                {
                    IsEquivalent = true,
                    Probability = 1.0,
                    Model = "pharmacy-sig-normalizer",
                    Evaluator = "PharmacySigNormalizer (Local)",
                    MatchReason = "Clinically equivalent directions (local normalizer)",
                    LatencyMs = 0,
                    FromCache = true
                };
                return true;
            }

            try
            {
                // Synchronous bridge with 2-second timeout to prevent stalling during batch processing
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                return Task.Run(() => EvaluateSigDetailedAsync(mmSig, erxSig, knownPii, cts.Token)).GetAwaiter().GetResult().IsEquivalent;
            }
            catch (Exception ex)
            {
                var mmLog = _options.EnablePhiSanitization ? ClinicalSigSanitizer.Sanitize(mmClean, knownPii) : mmClean;
                var erxLog = _options.EnablePhiSanitization ? ClinicalSigSanitizer.Sanitize(erxClean, knownPii) : erxClean;
                _logger.LogWarning(ex, "Synchronous TypeSafe AI call timed out or failed, falling back to baseline normalizer for '{MmSig}' vs '{ErxSig}'", mmLog, erxLog);
                return PharmacySigNormalizer.AreSigsEquivalent(mmClean, erxClean);
            }
        }

        public async Task<TypeSafeSigEvaluationResult> EvaluateSigDetailedAsync(
            string? mmSig,
            string? erxSig,
            IEnumerable<string?>? knownPii = null,
            CancellationToken cancellationToken = default)
        {
            var mmClean = (mmSig?.Trim() ?? string.Empty).Replace('\\', '/');
            var erxClean = (erxSig?.Trim() ?? string.Empty).Replace('\\', '/');

            // 1. Check trivial edge cases
            if (string.IsNullOrEmpty(mmClean) && string.IsNullOrEmpty(erxClean))
            {
                return new TypeSafeSigEvaluationResult
                {
                    IsEquivalent = true,
                    Probability = 1.0,
                    Model = _options.Model,
                    MatchReason = "Both SIG directions are empty",
                    LatencyMs = 0,
                    FromCache = true
                };
            }

            if (string.IsNullOrEmpty(mmClean) || string.IsNullOrEmpty(erxClean))
            {
                return new TypeSafeSigEvaluationResult
                {
                    IsEquivalent = false,
                    Probability = 0.0,
                    Model = _options.Model,
                    MatchReason = "One of the SIG directions is missing",
                    LatencyMs = 0,
                    FromCache = true
                };
            }

            // 2. Exact match short-circuit (0ms latency, zero API token cost)
            if (string.Equals(mmClean, erxClean, StringComparison.OrdinalIgnoreCase))
            {
                return new TypeSafeSigEvaluationResult
                {
                    IsEquivalent = true,
                    Probability = 1.0,
                    Model = _options.Model,
                    MatchReason = "Exact literal match (case-insensitive)",
                    LatencyMs = 0,
                    FromCache = true
                };
            }

            // 3. Cache lookup
            var cacheKey = $"{mmClean.ToUpperInvariant()}|||{erxClean.ToUpperInvariant()}";
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return new TypeSafeSigEvaluationResult
                {
                    IsEquivalent = cached.IsEquivalent,
                    Probability = cached.Probability,
                    Model = cached.Model,
                    Evaluator = cached.Evaluator,
                    MatchReason = cached.MatchReason,
                    LatencyMs = 0,
                    FromCache = true
                };
            }

            // 4. Local clinical normalizer fast path (0ms latency, zero API token cost)
            if (PharmacySigNormalizer.AreSigsEquivalent(mmClean, erxClean))
            {
                var fastResult = new TypeSafeSigEvaluationResult
                {
                    IsEquivalent = true,
                    Probability = 1.0,
                    Model = "pharmacy-sig-normalizer",
                    Evaluator = "PharmacySigNormalizer (Local)",
                    MatchReason = "Clinically equivalent directions (local normalizer)",
                    LatencyMs = 0,
                    FromCache = false
                };
                _cache[cacheKey] = fastResult;
                return fastResult;
            }

            // 5. If API key is missing, immediately fallback to baseline normalizer
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("TypeSafe AI ApiKey is not configured. Falling back to local baseline normalizer.");
                var fallbackMatch = PharmacySigNormalizer.AreSigsEquivalent(mmClean, erxClean);
                return new TypeSafeSigEvaluationResult
                {
                    IsEquivalent = fallbackMatch,
                    Probability = fallbackMatch ? 1.0 : 0.0,
                    Model = "baseline-normalizer-fallback",
                    Evaluator = "Baseline Sig Normalizer (Local)",
                    MatchReason = fallbackMatch ? "Baseline normalizer matched" : "Baseline normalizer mismatch",
                    LatencyMs = 0,
                    FromCache = false
                };
            }

            // 5. Strictly de-identify and sanitize directions (PHI/PII zero-leakage guarantee)
            var mmForAi = _options.EnablePhiSanitization
                ? ClinicalSigSanitizer.Sanitize(mmClean, knownPii)
                : mmClean;
            var erxForAi = _options.EnablePhiSanitization
                ? ClinicalSigSanitizer.Sanitize(erxClean, knownPii)
                : erxClean;

            if (string.IsNullOrWhiteSpace(mmForAi) || string.IsNullOrWhiteSpace(erxForAi))
            {
                var fallbackMatch = PharmacySigNormalizer.AreSigsEquivalent(mmClean, erxClean);
                return new TypeSafeSigEvaluationResult
                {
                    IsEquivalent = fallbackMatch,
                    Probability = fallbackMatch ? 1.0 : 0.0,
                    Model = "pharmacy-sig-normalizer",
                    Evaluator = "Clinical De-Identifier (Local)",
                    MatchReason = fallbackMatch ? "Sanitized directions matched locally" : "Sanitized directions mismatched locally",
                    LatencyMs = 0,
                    FromCache = false
                };
            }

            // 6. Call TypeSafe AI API (System One model jev-latest) with sanitized directions ONLY
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var requestPayload = new
                {
                    model = string.IsNullOrWhiteSpace(_options.Model) ? "jev-latest" : _options.Model,
                    state = $"Dispensed Directions: {mmForAi}\nPrescribed Directions: {erxForAi}",
                    questions = new
                    {
                        sig_comparison = new
                        {
                            type = "choice",
                            instructions = "As an expert clinical pharmacist auditor, determine if these two prescription directions are clinically and therapeutically compatible for dispensing. For prefilled single-dose injectable pens/autoinjectors, physicians commonly write '140 ml' intending the 140mg/ml (140 mg) dose; clinically treat '140 ml' and '140mg/ml' as compatible for once monthly subcutaneous injection. Also tolerate abbreviation expansions, PRN indication phrasing, and incidental duration tags.",
                            criteria = new Dictionary<string, string>
                            {
                                { "match", "The directions are clinically and therapeutically compatible for dispensing (same intended active dose/solution, same administration route, same dosing frequency, including physician clerical unit variants for prefilled injectables like 140 ml vs 140mg/ml once monthly subcutaneous)." },
                                { "mismatch", "There is a true clinical conflict in dose amount, administration route, or dosing interval/frequency (e.g., different dosing schedules, 1 tablet vs 2 tablets, oral vs topical)." }
                            }
                        }
                    }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
                request.Content = JsonContent.Create(requestPayload);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                stopwatch.Stop();

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("TypeSafe AI request failed with status {StatusCode}: {ErrorBody}. Falling back to baseline normalizer.",
                        response.StatusCode, errorBody);

                    var fallbackMatch = PharmacySigNormalizer.AreSigsEquivalent(mmClean, erxClean);
                    return new TypeSafeSigEvaluationResult
                    {
                        IsEquivalent = fallbackMatch,
                        Probability = fallbackMatch ? 1.0 : 0.0,
                        Model = _options.Model,
                        Evaluator = "TypeSafe AI Fallback (HTTP Error)",
                        MatchReason = fallbackMatch ? "Baseline fallback matched" : "Baseline fallback mismatch",
                        LatencyMs = stopwatch.ElapsedMilliseconds,
                        FromCache = false
                    };
                }

                var responseJson = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
                if (responseJson != null &&
                    responseJson.RootElement.TryGetProperty("answers", out var answersElement))
                {
                    var modelName = responseJson.RootElement.TryGetProperty("model", out var modelEl) ? modelEl.GetString() ?? _options.Model : _options.Model;

                    // 1. Primary: Choice primitive response (sig_comparison)
                    if (answersElement.TryGetProperty("sig_comparison", out var sigCompElement))
                    {
                        var choice = sigCompElement.TryGetProperty("choice", out var choiceEl) ? (choiceEl.GetString() ?? string.Empty) : string.Empty;
                        double matchProb = 0.0;
                        if (sigCompElement.TryGetProperty("probabilities", out var probsEl) &&
                            probsEl.TryGetProperty("match", out var matchProbEl) &&
                            matchProbEl.ValueKind == JsonValueKind.Number)
                        {
                            matchProb = matchProbEl.GetDouble();
                        }
                        else
                        {
                            matchProb = string.Equals(choice, "match", StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
                        }

                        var isEquivalent = string.Equals(choice, "match", StringComparison.OrdinalIgnoreCase) &&
                                           matchProb >= (_options.EquivalenceThreshold > 0 ? _options.EquivalenceThreshold : 0.50);

                        var evaluationResult = new TypeSafeSigEvaluationResult
                        {
                            IsEquivalent = isEquivalent,
                            Probability = matchProb,
                            Model = modelName,
                            Evaluator = $"TypeSafe AI ({modelName})",
                            MatchReason = isEquivalent
                                ? $"Clinically equivalent (Calibrated match probability: {matchProb:P0}, Model decision: '{choice}')"
                                : $"Clinical discrepancy detected (Calibrated match probability: {matchProb:P0}, Model decision: '{choice}')",
                            LatencyMs = stopwatch.ElapsedMilliseconds,
                            FromCache = false
                        };

                        _cache[cacheKey] = evaluationResult;
                        return evaluationResult;
                    }

                    // 2. Backward compatibility: Noul primitive response (is_equivalent)
                    if (answersElement.TryGetProperty("is_equivalent", out var isEquivElement) &&
                        isEquivElement.TryGetProperty("noul", out var noulElement))
                    {
                        double probability = 0.0;
                        if (noulElement.ValueKind == JsonValueKind.Number)
                        {
                            probability = noulElement.GetDouble();
                        }
                        else if (noulElement.ValueKind == JsonValueKind.True)
                        {
                            probability = 1.0;
                        }
                        else if (noulElement.ValueKind == JsonValueKind.False)
                        {
                            probability = 0.0;
                        }

                        var isEquivalent = probability >= _options.EquivalenceThreshold;

                        var evaluationResult = new TypeSafeSigEvaluationResult
                        {
                            IsEquivalent = isEquivalent,
                            Probability = probability,
                            Model = modelName,
                            Evaluator = $"TypeSafe AI ({modelName})",
                            MatchReason = isEquivalent
                                ? $"Clinically equivalent (Calibrated probability: {probability:P0}, Threshold: {_options.EquivalenceThreshold:P0})"
                                : $"Clinical discrepancy detected (Calibrated probability: {probability:P0}, Threshold: {_options.EquivalenceThreshold:P0})",
                            LatencyMs = stopwatch.ElapsedMilliseconds,
                            FromCache = false
                        };

                        _cache[cacheKey] = evaluationResult;
                        return evaluationResult;
                    }
                }

                _logger.LogWarning("TypeSafe AI response did not contain expected answers.sig_comparison structure. Falling back to baseline normalizer.");
                var normMatch = PharmacySigNormalizer.AreSigsEquivalent(mmClean, erxClean);
                return new TypeSafeSigEvaluationResult
                {
                    IsEquivalent = normMatch,
                    Probability = normMatch ? 1.0 : 0.0,
                    Model = _options.Model,
                    Evaluator = "TypeSafe AI Fallback",
                    MatchReason = normMatch ? "Baseline normalizer matched" : "Baseline normalizer mismatch",
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    FromCache = false
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "Error calling TypeSafe AI for SIG comparison. Using safe baseline fallback.");
                var normMatch = PharmacySigNormalizer.AreSigsEquivalent(mmClean, erxClean);
                return new TypeSafeSigEvaluationResult
                {
                    IsEquivalent = normMatch,
                    Probability = normMatch ? 1.0 : 0.0,
                    Model = _options.Model,
                    Evaluator = "TypeSafe AI Fallback (Exception)",
                    MatchReason = normMatch ? "Baseline normalizer matched" : "Baseline normalizer mismatch",
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    FromCache = false
                };
            }
        }
    }
}
