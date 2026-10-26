namespace Rxintel.Generic.Services.TypeSafeAI
{
    public class TypeSafeClientOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.typesafe.ai/v1/systemone";
        public string Model { get; set; } = "jev-latest";
        public double EquivalenceThreshold { get; set; } = 0.70;
        public int TimeoutSeconds { get; set; } = 15;

        /// <summary>
        /// When true (default), strictly de-identifies and redacts all PHI, PII, patient names, dates,
        /// phone numbers, addresses, and Rx numbers before any request reaches the external AI endpoint.
        /// </summary>
        public bool EnablePhiSanitization { get; set; } = true;
    }
}
