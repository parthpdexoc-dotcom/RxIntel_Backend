namespace Rxintel.Generic.Services.AI
{
    public class AiDrugEvaluationResult
    {
        public bool IsEquivalent { get; set; }
        public double Probability { get; set; }
        public string Model { get; set; } = string.Empty;
        public string Evaluator { get; set; } = "AI Drug Engine";
        public string MatchReason { get; set; } = string.Empty;
        public long LatencyMs { get; set; }
        public bool FromCache { get; set; }
    }

    public class AiComplianceEvaluationResult
    {
        public bool IsCompliant { get; set; }
        public double Probability { get; set; }
        public string Model { get; set; } = string.Empty;
        public string Evaluator { get; set; } = "AI Compliance Engine";
        public string MatchReason { get; set; } = string.Empty;
        public string? DetectedDeaSchedule { get; set; }
        public long LatencyMs { get; set; }
        public bool FromCache { get; set; }
    }
}
