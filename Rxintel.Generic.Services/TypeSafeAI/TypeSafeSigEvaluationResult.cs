namespace Rxintel.Generic.Services.TypeSafeAI
{
    public class TypeSafeSigEvaluationResult
    {
        public bool IsEquivalent { get; set; }
        public double Probability { get; set; }
        public string Model { get; set; } = string.Empty;
        public string Evaluator { get; set; } = "TypeSafe AI (jev-latest)";
        public string MatchReason { get; set; } = string.Empty;
        public long LatencyMs { get; set; }
        public bool FromCache { get; set; }
    }
}
