namespace Rxintel.Generic.Services.MetaAI
{
    public class MetaAiClientOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.meta.ai/v1";
        public string Model { get; set; } = "muse-spark-1.3-contributor";
        public double EquivalenceThreshold { get; set; } = 0.70;
        public int TimeoutSeconds { get; set; } = 15;
        public bool EnablePhiSanitization { get; set; } = true;
    }
}
