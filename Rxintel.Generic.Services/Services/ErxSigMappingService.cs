using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Helpers;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services
{
    public class ErxSigMappingService : IErxSigMappingService
    {
        private readonly IErxSigMappingRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<ErxSigMappingService> _logger;
        private readonly ITypeSafeClient? _typeSafeClient;
        private readonly IMetaAiClient? _metaAiClient;

        // Thread-safe in-memory cache of active mappings
        private static List<ErxSigMappingDto>? _cachedMappings;
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);
        private static DateTime _lastCacheTime = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

        public ErxSigMappingService(
            IErxSigMappingRepository repository,
            IMapper mapper,
            ILogger<ErxSigMappingService> logger,
            ITypeSafeClient? typeSafeClient = null,
            IMetaAiClient? metaAiClient = null)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _typeSafeClient = typeSafeClient;
            _metaAiClient = metaAiClient;
        }

        public async Task<PagedList<ErxSigMappingDto>> GetAllMappingsAsync(PageParam pageParam, string? searchTerm = null, string? category = null)
        {
            await EnsureCacheLoadedAsync();

            pageParam ??= new PageParam();
            var all = await _repository.GetAllAsync();
            var query = all.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            var effectiveSearch = !string.IsNullOrWhiteSpace(searchTerm)
                ? searchTerm
                : pageParam.SearchText;

            if (!string.IsNullOrWhiteSpace(effectiveSearch))
            {
                var term = effectiveSearch.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    (!string.IsNullOrEmpty(x.Abbreviation) && x.Abbreviation.ToLowerInvariant().Contains(term)) ||
                    (!string.IsNullOrEmpty(x.SimpleEnglish) && x.SimpleEnglish.ToLowerInvariant().Contains(term)) ||
                    (!string.IsNullOrEmpty(x.NormalizedToken) && x.NormalizedToken.ToLowerInvariant().Contains(term)) ||
                    (!string.IsNullOrEmpty(x.ClinicalContext) && x.ClinicalContext.ToLowerInvariant().Contains(term)));
            }

            var entities = query
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Abbreviation)
                .ToList();

            var totalRecords = entities.Count;
            var page = pageParam.Page is > 0 ? pageParam.Page.Value : 1;
            var pageSize = pageParam.PageSize is > 0 ? pageParam.PageSize.Value : 10;
            var pagedEntities = entities
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var pageParamForResult = new PageParam
            {
                Page = page,
                PageSize = pageSize,
                SearchText = effectiveSearch,
                SortBy = pageParam.SortBy,
                SortDirection = pageParam.SortDirection
            };

            return new PagedList<ErxSigMappingDto>(
                _mapper.Map<List<ErxSigMappingDto>>(pagedEntities),
                pageParamForResult,
                totalRecords);
        }

        public async Task<ErxSigMappingDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id, "Id");
            return entity == null ? null : _mapper.Map<ErxSigMappingDto>(entity);
        }

        public async Task<SuccessResult<ErxSigMappingDto>> CreateMappingAsync(CreateErxSigMappingDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Abbreviation))
                return SuccessResult<ErxSigMappingDto>.BadRequest("Abbreviation is required.");

            if (string.IsNullOrWhiteSpace(dto.SimpleEnglish))
                return SuccessResult<ErxSigMappingDto>.BadRequest("Simple English statement is required.");

            var normalizedToken = string.IsNullOrWhiteSpace(dto.NormalizedToken)
                ? dto.SimpleEnglish.Trim().ToLowerInvariant()
                : dto.NormalizedToken.Trim().ToLowerInvariant();

            var entity = new ErxSigMapping
            {
                Abbreviation = dto.Abbreviation.Trim().ToUpperInvariant(),
                SimpleEnglish = dto.SimpleEnglish.Trim(),
                NormalizedToken = normalizedToken,
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "Custom" : dto.Category.Trim(),
                ClinicalContext = dto.ClinicalContext?.Trim(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _repository.UpsertAsync(entity, "Id");
            await InvalidateCacheAsync();

            var resultDto = _mapper.Map<ErxSigMappingDto>(entity);
            return SuccessResult<ErxSigMappingDto>.Ok(resultDto, "SIG mapping created successfully.");
        }

        public async Task<SuccessResult<ErxSigMappingDto>> UpdateMappingAsync(int id, UpdateErxSigMappingDto dto)
        {
            var entity = await _repository.GetByIdAsync(id, "Id");
            if (entity == null)
                return SuccessResult<ErxSigMappingDto>.NotFound($"SIG mapping with ID {id} not found.");

            if (string.IsNullOrWhiteSpace(dto.Abbreviation))
                return SuccessResult<ErxSigMappingDto>.BadRequest("Abbreviation is required.");

            if (string.IsNullOrWhiteSpace(dto.SimpleEnglish))
                return SuccessResult<ErxSigMappingDto>.BadRequest("Simple English statement is required.");

            var normalizedToken = string.IsNullOrWhiteSpace(dto.NormalizedToken)
                ? dto.SimpleEnglish.Trim().ToLowerInvariant()
                : dto.NormalizedToken.Trim().ToLowerInvariant();

            entity.Abbreviation = dto.Abbreviation.Trim().ToUpperInvariant();
            entity.SimpleEnglish = dto.SimpleEnglish.Trim();
            entity.NormalizedToken = normalizedToken;
            entity.Category = string.IsNullOrWhiteSpace(dto.Category) ? "Custom" : dto.Category.Trim();
            entity.ClinicalContext = dto.ClinicalContext?.Trim();
            entity.IsActive = dto.IsActive;
            entity.ModifiedDate = DateTime.UtcNow;

            await _repository.UpsertAsync(entity, "Id");
            await InvalidateCacheAsync();

            var resultDto = _mapper.Map<ErxSigMappingDto>(entity);
            return SuccessResult<ErxSigMappingDto>.Ok(resultDto, "SIG mapping updated successfully.");
        }

        public async Task<SuccessResult<string>> DeleteMappingAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id, "Id");
            if (entity == null)
                return SuccessResult<string>.NotFound($"SIG mapping with ID {id} not found.");

            await _repository.DeleteAsync(id);
            await InvalidateCacheAsync();

            return SuccessResult<string>.Ok(id.ToString(), "SIG mapping deleted successfully.");
        }

        public async Task<SuccessResult<string>> ToggleActiveAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id, "Id");
            if (entity == null)
                return SuccessResult<string>.NotFound($"SIG mapping with ID {id} not found.");

            entity.IsActive = !entity.IsActive;
            entity.ModifiedDate = DateTime.UtcNow;

            await _repository.UpsertAsync(entity, "Id");
            await InvalidateCacheAsync();

            var status = entity.IsActive ? "activated" : "deactivated";
            return SuccessResult<string>.Ok(entity.IsActive.ToString(), $"SIG mapping {entity.Abbreviation} {status}.");
        }

        public async Task<TestSigComparisonResponseDto> TestComparisonAsync(TestSigComparisonRequestDto dto)
        {
            var erxRaw = dto.ErxSig?.Trim() ?? string.Empty;
            var mmRaw = dto.MmSig?.Trim() ?? string.Empty;

            var erxNorm = PharmacySigNormalizer.NormalizeSig(erxRaw);
            var mmNorm = PharmacySigNormalizer.NormalizeSig(mmRaw);

            var erxSimple = PharmacySigNormalizer.ToSimpleEnglishSentence(erxRaw);
            var mmSimple = PharmacySigNormalizer.ToSimpleEnglishSentence(mmRaw);

            var preferredProvider = dto.Provider?.Trim() ?? "Auto";

            // 1. Meta AI execution
            if ((preferredProvider.Equals("MetaAI", StringComparison.OrdinalIgnoreCase) || preferredProvider.Equals("Meta", StringComparison.OrdinalIgnoreCase) ||
                 (preferredProvider.Equals("Auto", StringComparison.OrdinalIgnoreCase) && _metaAiClient != null && _metaAiClient.IsConfigured)) &&
                _metaAiClient != null)
            {
                var metaResult = await _metaAiClient.EvaluateSigAsync(mmRaw, erxRaw);
                return new TestSigComparisonResponseDto
                {
                    ErxSigRaw = erxRaw,
                    MmSigRaw = mmRaw,
                    ErxNormalized = erxNorm,
                    MmNormalized = mmNorm,
                    ErxSimpleEnglish = erxSimple,
                    MmSimpleEnglish = mmSimple,
                    IsEquivalent = metaResult.IsCompliant,
                    MatchReason = metaResult.Reason,
                    ConfidenceScore = metaResult.Confidence,
                    Evaluator = metaResult.Evaluator,
                    LatencyMs = metaResult.LatencyMs
                };
            }

            // 2. TypeSafe AI execution
            if (_typeSafeClient != null)
            {
                var aiResult = await _typeSafeClient.EvaluateSigDetailedAsync(mmRaw, erxRaw);
                return new TestSigComparisonResponseDto
                {
                    ErxSigRaw = erxRaw,
                    MmSigRaw = mmRaw,
                    ErxNormalized = erxNorm,
                    MmNormalized = mmNorm,
                    ErxSimpleEnglish = erxSimple,
                    MmSimpleEnglish = mmSimple,
                    IsEquivalent = aiResult.IsEquivalent,
                    MatchReason = aiResult.MatchReason,
                    ConfidenceScore = aiResult.Probability,
                    Evaluator = aiResult.Evaluator,
                    LatencyMs = aiResult.LatencyMs
                };
            }

            var isEquivalent = PharmacySigNormalizer.AreSigsEquivalent(mmRaw, erxRaw);
            return new TestSigComparisonResponseDto
            {
                ErxSigRaw = erxRaw,
                MmSigRaw = mmRaw,
                ErxNormalized = erxNorm,
                MmNormalized = mmNorm,
                ErxSimpleEnglish = erxSimple,
                MmSimpleEnglish = mmSimple,
                IsEquivalent = isEquivalent,
                MatchReason = isEquivalent
                    ? "Compatible clinical match: the directions align on core dosing, route, and schedule."
                    : "Clinical discrepancy: detected conflicting dosage quantity, frequency, or administration route.",
                ConfidenceScore = isEquivalent ? 1.0 : 0.0,
                Evaluator = "Local Baseline Normalizer",
                LatencyMs = 0
            };
        }

        public async Task<bool> AreSigsEquivalentAsync(string? sig1, string? sig2)
        {
            if (_metaAiClient != null && _metaAiClient.IsConfigured)
            {
                var metaRes = await _metaAiClient.EvaluateSigAsync(sig1, sig2);
                if (metaRes.IsCompliant) return true;
            }

            if (_typeSafeClient != null)
            {
                return await _typeSafeClient.AreSigsEquivalentAsync(sig1, sig2);
            }
            await EnsureCacheLoadedAsync();
            return PharmacySigNormalizer.AreSigsEquivalent(sig1, sig2);
        }

        public async Task InvalidateCacheAsync()
        {
            await _cacheLock.WaitAsync();
            try
            {
                _cachedMappings = null;
                _lastCacheTime = DateTime.MinValue;
            }
            finally
            {
                _cacheLock.Release();
            }

            await EnsureCacheLoadedAsync();
        }

        public async Task EnsureCacheLoadedAsync()
        {
            if (_cachedMappings != null && DateTime.UtcNow - _lastCacheTime < CacheDuration)
                return;

            await _cacheLock.WaitAsync();
            try
            {
                if (_cachedMappings != null && DateTime.UtcNow - _lastCacheTime < CacheDuration)
                    return;

                var activeEntities = (await _repository.GetAllAsync(x => x.IsActive))
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.Abbreviation)
                    .ToList();
                _cachedMappings = _mapper.Map<List<ErxSigMappingDto>>(activeEntities);
                _lastCacheTime = DateTime.UtcNow;

                // Sync dynamic mappings into PharmacySigNormalizer
                SyncToNormalizer(_cachedMappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading active SIG mappings into cache.");
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private static void SyncToNormalizer(List<ErxSigMappingDto> mappings)
        {
            if (mappings == null || mappings.Count == 0) return;

            var tokenDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var phraseList = new List<(string Abbr, string Expansion)>();
            var frequencyList = new List<string>();

            foreach (var m in mappings)
            {
                if (string.IsNullOrWhiteSpace(m.Abbreviation)) continue;

                var abbr = m.Abbreviation.Trim();
                var norm = string.IsNullOrWhiteSpace(m.NormalizedToken) ? m.SimpleEnglish.Trim().ToLowerInvariant() : m.NormalizedToken.Trim().ToLowerInvariant();

                // If abbreviation has spaces or slashes, treat as phrase
                if (abbr.Contains(' ') || abbr.Contains('/'))
                {
                    phraseList.Add((abbr, norm));
                }
                else
                {
                    tokenDict[abbr] = norm;
                }

                if (m.Category != null && m.Category.StartsWith("Frequency", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(norm))
                {
                    frequencyList.Add(norm);
                }
            }

            PharmacySigNormalizer.UpdateDynamicMappings(tokenDict, phraseList, frequencyList);
        }
    }
}
