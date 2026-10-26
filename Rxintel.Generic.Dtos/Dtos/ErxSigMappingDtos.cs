using System;

namespace Rxintel.Generic.Dtos.Dtos
{
    public class ErxSigMappingDto
    {
        public int Id { get; set; }
        public string Abbreviation { get; set; } = string.Empty;
        public string SimpleEnglish { get; set; } = string.Empty;
        public string NormalizedToken { get; set; } = string.Empty;
        public string? Category { get; set; } = "General";
        public string? ClinicalContext { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class CreateErxSigMappingDto
    {
        public string Abbreviation { get; set; } = string.Empty;
        public string SimpleEnglish { get; set; } = string.Empty;
        public string? NormalizedToken { get; set; }
        public string? Category { get; set; } = "General";
        public string? ClinicalContext { get; set; }
    }

    public class UpdateErxSigMappingDto
    {
        public string Abbreviation { get; set; } = string.Empty;
        public string SimpleEnglish { get; set; } = string.Empty;
        public string? NormalizedToken { get; set; }
        public string? Category { get; set; } = "General";
        public string? ClinicalContext { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class TestSigComparisonRequestDto
    {
        public string ErxSig { get; set; } = string.Empty;
        public string MmSig { get; set; } = string.Empty;
        public string? Provider { get; set; } = "Auto";
    }

    public class TestSigComparisonResponseDto
    {
        public string ErxSigRaw { get; set; } = string.Empty;
        public string MmSigRaw { get; set; } = string.Empty;
        public string ErxNormalized { get; set; } = string.Empty;
        public string MmNormalized { get; set; } = string.Empty;
        public string ErxSimpleEnglish { get; set; } = string.Empty;
        public string MmSimpleEnglish { get; set; } = string.Empty;
        public bool IsEquivalent { get; set; }
        public string MatchReason { get; set; } = string.Empty;
        public double? ConfidenceScore { get; set; }
        public string? Evaluator { get; set; }
        public long? LatencyMs { get; set; }
    }
}
