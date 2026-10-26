using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rxintel.Generic.Services.MetaAI;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IMetaAiClient
    {
        bool IsConfigured { get; }

        Task<MetaAiEvaluationResult> EvaluateSigAsync(
            string? mmSig,
            string? erxSig,
            IEnumerable<string?>? knownPii = null,
            CancellationToken cancellationToken = default);

        bool AreSigsEquivalent(
            string? mmSig,
            string? erxSig,
            IEnumerable<string?>? knownPii = null);

        Task<MetaAiEvaluationResult> EvaluateDaysSupplyAsync(
            int mmDaysSupply,
            int erxDaysSupply,
            string? sig,
            string? drugName,
            decimal? qtyDispensed,
            IEnumerable<string?>? knownPii = null,
            CancellationToken cancellationToken = default);

        bool IsDaysSupplyCompliant(
            int mmDaysSupply,
            int erxDaysSupply,
            string? sig,
            string? drugName,
            decimal? qtyDispensed,
            IEnumerable<string?>? knownPii = null);

        Task<MetaAiEvaluationResult> EvaluateQuantityDispensedAsync(
            decimal mmQtyDispensed,
            decimal erxQtyOrd,
            string? sig,
            string? drugName,
            int? daysSupply,
            IEnumerable<string?>? knownPii = null,
            CancellationToken cancellationToken = default);

        bool IsQuantityDispensedCompliant(
            decimal mmQtyDispensed,
            decimal erxQtyOrd,
            string? sig,
            string? drugName,
            int? daysSupply,
            IEnumerable<string?>? knownPii = null);

        Task<MetaAiPrescriptionAuditResult> EvaluatePrescriptionAuditAsync(
            MetaAiPrescriptionAuditRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves or generates a HIPAA / DEA / CMS fix suggestion for a prescription discrepancy using Muse AI.
        /// </summary>
        Task<MuseDiscrepancySuggestionResult> GetDiscrepancySuggestionAsync(
            MuseDiscrepancySuggestionRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records or updates an auditor-approved or AI-deduced clinical fact in Muse's permanent memory.
        /// </summary>
        MuseMemoryEntry UpdateMemory(MuseMemoryEntry entry);

        /// <summary>
        /// Retrieves statistical summary of Muse's active clinical knowledge memory.
        /// </summary>
        MuseMemorySummaryDto GetMemorySummary();

        /// <summary>
        /// Retrieves all remembered clinical equivalences and rules filtered by category and search term.
        /// </summary>
        List<MuseMemoryEntry> GetAllMemories(string? category = null, string? search = null);

        /// <summary>
        /// Deletes a specific memory entry from Muse's memory store.
        /// </summary>
        bool DeleteMemory(string key);
    }
}
