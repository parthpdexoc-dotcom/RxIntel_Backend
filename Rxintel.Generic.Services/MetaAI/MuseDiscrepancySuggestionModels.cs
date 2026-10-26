using System;
using System.Collections.Generic;

namespace Rxintel.Generic.Services.MetaAI
{
    /// <summary>
    /// Request payload for asking Muse AI to evaluate a prescription discrepancy
    /// and generate a compliant HIPAA / DEA / CMS fix suggestion for the pharmacy owner.
    /// </summary>
    public class MuseDiscrepancySuggestionRequest
    {
        public string FieldName { get; set; } = string.Empty;
        public string? MmValue { get; set; }
        public string? ErxValue { get; set; }
        public string? DiscrepancyType { get; set; }
        public string? RxNumber { get; set; }
        public string? PatientName { get; set; }
        public string? PatientDob { get; set; }
        public string? PrescriberName { get; set; }
        public string? PrescriberNpi { get; set; }
        public string? PrescriberDea { get; set; }
        public string? SupervisorName { get; set; }
        public string? DrugName { get; set; }
        public string? Ndc { get; set; }
        public string? Sig { get; set; }
        public string? State { get; set; }
        public int? RuleId { get; set; }
    }

    /// <summary>
    /// Response payload containing HIPAA guidelines, regulatory citations,
    /// clinical explanations, and the exact suggested fix value for the pharmacy owner.
    /// </summary>
    public class MuseDiscrepancySuggestionResult
    {
        public string Title { get; set; } = "HIPAA Fix Suggestion";
        public List<string> Badges { get; set; } = new();
        public string Explanation { get; set; } = string.Empty;
        public string SuggestedFix { get; set; } = string.Empty;
        public string? RegulatoryRisk { get; set; }
        public string? DetailedInfo { get; set; }
        public double Confidence { get; set; } = 1.0;
        public string Source { get; set; } = "Muse AI (HIPAA Engine)";
        public bool FromCache { get; set; }
    }
}
