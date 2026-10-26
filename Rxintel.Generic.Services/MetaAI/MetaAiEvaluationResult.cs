namespace Rxintel.Generic.Services.MetaAI
{
    public class MetaAiEvaluationResult
    {
        public bool IsCompliant { get; set; }
        public double Confidence { get; set; }
        public string Model { get; set; } = string.Empty;
        public string Evaluator { get; set; } = "Meta AI";
        public string Reason { get; set; } = string.Empty;
        public long LatencyMs { get; set; }
        public bool FromCache { get; set; }
    }
}
