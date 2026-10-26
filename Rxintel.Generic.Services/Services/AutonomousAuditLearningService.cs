using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Helpers;
using Rxintel.Generic.Services.Interfaces;
using Rxintel.Generic.Services.MetaAI;

namespace Rxintel.Generic.Services.Services
{
    public class AutonomousAuditLearningService : IAutonomousAuditLearningService
    {
        private readonly IErxAuditBatchRepository _batchRepository;
        private readonly IErxAuditRecordRepository _recordRepository;
        private readonly IErxColumnMatchRuleRepository _ruleRepository;
        private readonly IErxAuditDiscrepancyRepository _discrepancyRepository;
        private readonly IMetaAiClient _metaAiClient;
        private readonly IDeaScheduleService _deaScheduleService;
        private readonly IErxReferenceDataService? _referenceDataService;
        private readonly RxintelDbContext? _dbContext;
        private readonly ILogger<AutonomousAuditLearningService> _logger;

        public AutonomousAuditLearningService(
            IErxAuditBatchRepository batchRepository,
            IErxAuditRecordRepository recordRepository,
            IErxColumnMatchRuleRepository ruleRepository,
            IErxAuditDiscrepancyRepository discrepancyRepository,
            IMetaAiClient metaAiClient,
            IDeaScheduleService deaScheduleService,
            ILogger<AutonomousAuditLearningService> logger,
            IErxReferenceDataService? referenceDataService = null,
            RxintelDbContext? dbContext = null)
        {
            _batchRepository = batchRepository;
            _recordRepository = recordRepository;
            _ruleRepository = ruleRepository;
            _discrepancyRepository = discrepancyRepository;
            _metaAiClient = metaAiClient;
            _deaScheduleService = deaScheduleService;
            _referenceDataService = referenceDataService;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<AutonomousLearningReportDto> RunAutonomousLearningAsync(
            Guid? batchId = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[AutonomousLearning] Starting zero-human-interaction audit sweep. Target Batch: {BatchId}...", batchId);

            var report = new AutonomousLearningReportDto
            {
                BatchId = batchId
            };

            var initialMemoryCount = MuseClinicalMemoryStore.GetSummary().TotalMemories;

            var includes = new Expression<Func<ErxAuditRecord, object>>[]
            {
                r => r.Discrepancies
            };

            Expression<Func<ErxAuditRecord, bool>> filter = r => !batchId.HasValue || r.BatchId == batchId.Value;
            var records = (await _recordRepository.GetAllAsync(filter, includes)).ToList();
            var rulesDict = (await _ruleRepository.GetAllAsync()).ToDictionary(r => r.RuleId);

            report.TotalRecordsScanned = records.Count;

            var resolvedDiscrepancies = new List<ErxAuditDiscrepancy>();
            var affectedBatchIds = new HashSet<Guid>();

            foreach (var record in records)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var openDiscrepancies = record.Discrepancies
                    .Where(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status))
                    .ToList();

                if (openDiscrepancies.Count == 0) continue;

                report.TotalDiscrepanciesEvaluated += openDiscrepancies.Count;

                // Clinical pre-computations for the record
                var hasMatchingDemographics = ErxComparisonEngine.ArePatientDemographicsMatching(record);
                var isControlled = (_referenceDataService?.IsControlledSubstance(record) ?? false) ||
                                   _deaScheduleService.IsClass2Narcotic(record) ||
                                   _deaScheduleService.IsClass3To5Narcotic(record);
                var hasMatchingNdc = ErxComparisonEngine.AreNdcsMatching(record) ||
                                     (_dbContext != null && PharmacyDrugNormalizer.AreNdcsEquivalent(record.MmDispensedNdc, record.ErxNdc, record, _dbContext));
                var isQtyOrderedCompliant = ErxComparisonEngine.IsQuantityOrderedCompliant(record);
                var isQtyDispensedCompliant = ErxComparisonEngine.IsQuantityDispensedCompliant(record, metaAiClient: _metaAiClient);
                var isDaysSupplyCompliant = ErxComparisonEngine.IsDaysSupplyCompliant(record, metaAiClient: _metaAiClient);

                foreach (var d in openDiscrepancies)
                {
                    if (!rulesDict.TryGetValue(d.RuleId, out var rule) || rule == null)
                        continue;

                    var resolved = false;
                    var reason = string.Empty;
                    var category = rule.FieldGroupName ?? "General";

                    // 1. Patient Name / Demographics
                    if (ErxComparisonEngine.IsPatientNameRule(rule))
                    {
                        if (hasMatchingDemographics ||
                            ErxComparisonEngine.ArePatientNamesClinicallyEquivalent(record.MmPatientName, record.ErxPatientName, record))
                        {
                            resolved = true;
                            reason = "System (Autonomous Muse AI: Patient Demographics & Diminutives Match)";
                            category = "PATIENT_NAME";

                            MuseClinicalMemoryStore.RecordResolutionMemory(
                                "PATIENT_NAME",
                                d.MmValue ?? record.MmPatientName ?? "",
                                d.ErxValue ?? record.ErxPatientName ?? "",
                                "Corrected",
                                "Autonomous Muse AI",
                                record.MmDispensedDrugName ?? record.ErxDrugName);
                        }
                    }
                    // 2. SIG / Directions
                    else if (rule.FieldGroupName.IndexOf("SIG", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             rule.MmColumnName.IndexOf("SIG", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var mmSig = d.MmValue ?? record.MmDispensedSig;
                        var erxSig = d.ErxValue ?? record.ErxSig;

                        if (!string.IsNullOrWhiteSpace(mmSig) && !string.IsNullOrWhiteSpace(erxSig))
                        {
                            if (PharmacySigNormalizer.AreSigsEquivalent(mmSig, erxSig))
                            {
                                resolved = true;
                                reason = "System (Autonomous Muse AI: Clinical Directions Normalized)";
                            }
                            else if (_metaAiClient.IsConfigured)
                            {
                                var sigEval = await _metaAiClient.EvaluateSigAsync(mmSig, erxSig, cancellationToken: cancellationToken);
                                if (sigEval.IsCompliant && sigEval.Confidence >= 0.70)
                                {
                                    resolved = true;
                                    reason = $"System (Autonomous Muse AI: {sigEval.Reason})";
                                }
                            }
                            category = "SIG";
                        }
                    }
                    // 3. Quantity Dispensed
                    else if (ErxComparisonEngine.IsQuantityDispensedRule(rule))
                    {
                        if (isQtyDispensedCompliant)
                        {
                            resolved = true;
                            reason = "System (Autonomous Muse AI: Packaging / Dose Math Equivalence)";
                            category = "QUANTITY";

                            MuseClinicalMemoryStore.RecordResolutionMemory(
                                "QUANTITY",
                                d.MmValue ?? record.MmQtyDispensed?.ToString() ?? "",
                                d.ErxValue ?? record.ErxQtyOrd?.ToString() ?? "",
                                "Corrected",
                                "Autonomous Muse AI",
                                record.MmDispensedDrugName ?? record.ErxDrugName);
                        }
                    }
                    // 4. Quantity Ordered
                    else if (ErxComparisonEngine.IsQuantityOrderedRule(rule))
                    {
                        if (isQtyOrderedCompliant)
                        {
                            resolved = true;
                            reason = "System (Autonomous Muse AI: Unit Package Size Equivalence)";
                            category = "QUANTITY";

                            MuseClinicalMemoryStore.RecordResolutionMemory(
                                "QUANTITY",
                                d.MmValue ?? record.MmQtyOrd?.ToString() ?? "",
                                d.ErxValue ?? record.ErxQtyOrd?.ToString() ?? "",
                                "Corrected",
                                "Autonomous Muse AI",
                                record.MmDispensedDrugName ?? record.ErxDrugName);
                        }
                    }
                    // 5. Days Supply
                    else if (ErxComparisonEngine.IsDaysSupplyRule(rule))
                    {
                        if (isDaysSupplyCompliant)
                        {
                            resolved = true;
                            reason = "System (Autonomous Muse AI: USP <797> Beyond-Use Date / Dosing Ratio)";
                            category = "DAYS_SUPPLY";

                            MuseClinicalMemoryStore.RecordResolutionMemory(
                                "DAYS_SUPPLY",
                                d.MmValue ?? record.MmDispensedDaysSupply?.ToString() ?? "",
                                d.ErxValue ?? record.ErxDaysSupply?.ToString() ?? "",
                                "Corrected",
                                "Autonomous Muse AI",
                                record.MmDispensedDrugName ?? record.ErxDrugName);
                        }
                    }
                    // 6. DEA Schedule for Non-Controlled Substance
                    else if (ErxComparisonEngine.IsDeaRule(rule))
                    {
                        if (!isControlled)
                        {
                            resolved = true;
                            reason = "System (Autonomous Muse AI: Non-Controlled Drug)";
                            category = "PRESCRIBER";
                        }
                    }
                    // 7. Drug Name & NDC
                    else if (ErxComparisonEngine.IsDrugNameRule(rule) || ErxComparisonEngine.IsNdcRule(rule))
                    {
                        if (hasMatchingNdc)
                        {
                            resolved = true;
                            reason = "System (Autonomous Muse AI: Medicine Master Verified NDC Equivalence)";
                            category = "DRUG_NDC";

                            MuseClinicalMemoryStore.RecordResolutionMemory(
                                "DRUG_NDC",
                                d.MmValue ?? record.MmDispensedDrugName ?? record.MmDispensedNdc ?? "",
                                d.ErxValue ?? record.ErxDrugName ?? record.ErxNdc ?? "",
                                "Corrected",
                                "Autonomous Muse AI",
                                record.MmDispensedDrugName ?? record.ErxDrugName);
                        }
                    }

                    if (resolved)
                    {
                        d.Status = "Corrected";
                        d.ReviewedBy = reason;
                        d.ReviewedAt = DateTime.UtcNow;

                        resolvedDiscrepancies.Add(d);
                        affectedBatchIds.Add(record.BatchId);

                        report.AutonomouslyResolvedCount++;
                        report.ResolvedByCategory[category] = report.ResolvedByCategory.GetValueOrDefault(category, 0) + 1;
                    }
                }
            }

            if (resolvedDiscrepancies.Count > 0)
            {
                await _discrepancyRepository.BulkUpsertAsync(resolvedDiscrepancies, "DiscrepancyId");

                // Recompute and persist remaining total discrepancies for all affected batches
                foreach (var bId in affectedBatchIds)
                {
                    var bRecords = (await _recordRepository.GetAllAsync(r => r.BatchId == bId, includes)).ToList();
                    var remainingBatchDiscs = bRecords.SelectMany(r => r.Discrepancies)
                        .Count(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status));

                    var batch = await _batchRepository.GetByIdAsync(bId, "BatchId");
                    if (batch != null)
                    {
                        batch.TotalDiscrepancies = remainingBatchDiscs;
                        await _batchRepository.UpsertAsync(batch, "BatchId");
                    }
                }
            }

            var finalMemoryCount = MuseClinicalMemoryStore.GetSummary().TotalMemories;
            report.NewMemoriesLearned = Math.Max(0, finalMemoryCount - initialMemoryCount);

            foreach (var kvp in report.ResolvedByCategory)
            {
                report.LearnedInsights.Add($"Autonomously eliminated {kvp.Value} discrepancy(ies) for '{kvp.Key}' without human intervention.");
            }

            _logger.LogInformation(
                "[AutonomousLearning] Complete. Scanned: {Scanned}, Evaluated: {Evaluated}, Autonomously Resolved: {Resolved}, New Memories: {NewMemories}.",
                report.TotalRecordsScanned, report.TotalDiscrepanciesEvaluated, report.AutonomouslyResolvedCount, report.NewMemoriesLearned);

            return report;
        }
    }
}
