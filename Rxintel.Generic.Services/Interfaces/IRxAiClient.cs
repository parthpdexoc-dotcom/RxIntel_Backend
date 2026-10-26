using System.Threading;
using System.Threading.Tasks;
using Rxintel.Generic.Services.AI;

namespace Rxintel.Generic.Services.Interfaces
{
    /// <summary>
    /// Dedicated interface for clinical AI model evaluations:
    /// - Drug Name, Brand-to-Generic, and Strength Equivalence
    /// - Regulatory and Prescription Compliance Checks
    /// Note: TypeSafe AI is maintained separately and untouched for SIG directions.
    /// </summary>
    public interface IRxAiClient
    {
        /// <summary>
        /// True if an API Key is configured and ready to execute AI evaluations.
        /// </summary>
        bool IsAiEnabled { get; }

        /// <summary>
        /// Current configuration options.
        /// </summary>
        AiModelOptions Options { get; }

        /// <summary>
        /// Asynchronously evaluates whether dispensed and prescribed drugs are clinically equivalent via AI.
        /// </summary>
        Task<bool> AreDrugsEquivalentAsync(
            string? mmDrug,
            string? erxDrug,
            string? mmStrength = null,
            string? erxStrength = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronously evaluates whether dispensed and prescribed drugs are clinically equivalent via AI.
        /// </summary>
        bool AreDrugsEquivalent(
            string? mmDrug,
            string? erxDrug,
            string? mmStrength = null,
            string? erxStrength = null);

        /// <summary>
        /// Detailed AI clinical evaluation of drug compatibility, including confidence score, evaluator name, and clinical reasoning.
        /// </summary>
        Task<AiDrugEvaluationResult> EvaluateDrugDetailedAsync(
            string? mmDrug,
            string? erxDrug,
            string? mmStrength = null,
            string? erxStrength = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Evaluates regulatory compliance (controlled substance schedule, days supply limit, state validity) via AI.
        /// </summary>
        Task<AiComplianceEvaluationResult> EvaluateComplianceAsync(
            string drugName,
            string ndc,
            int? daysSupply,
            int? refills,
            string state,
            CancellationToken cancellationToken = default);
    }
}
