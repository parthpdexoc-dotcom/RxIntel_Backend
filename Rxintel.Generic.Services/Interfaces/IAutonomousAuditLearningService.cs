using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rxintel.Generic.Services.Interfaces
{
    public class AutonomousLearningReportDto
    {
        public Guid? BatchId { get; set; }
        public int TotalRecordsScanned { get; set; }
        public int TotalDiscrepanciesEvaluated { get; set; }
        public int AutonomouslyResolvedCount { get; set; }
        public int NewMemoriesLearned { get; set; }
        public Dictionary<string, int> ResolvedByCategory { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> LearnedInsights { get; set; } = new();
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Service for autonomous, zero-human-interaction clinical auditing and continuous memory learning.
    /// Evaluates open discrepancies using Muse AI and clinical intelligence, records verified facts into
    /// Muse's permanent memory bank, and auto-resolves compliant records.
    /// </summary>
    public interface IAutonomousAuditLearningService
    {
        /// <summary>
        /// Executes an autonomous clinical audit sweep across all or specific batch records,
        /// training Muse AI's memory and auto-resolving false discrepancies without human clicks.
        /// </summary>
        Task<AutonomousLearningReportDto> RunAutonomousLearningAsync(Guid? batchId = null, CancellationToken cancellationToken = default);
    }
}
