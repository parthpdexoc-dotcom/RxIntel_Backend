namespace Rxintel.Generic.Services.AI
{
    /// <summary>
    /// Configuration options for the AI Model service (OpenAI / fine-tuned custom model).
    /// Used for clinical drug equivalence, generic substitution, and compliance verification.
    /// Note: TypeSafe AI is maintained separately and untouched for SIG directions.
    /// </summary>
    public class AiModelOptions
    {
        /// <summary>
        /// API Key for the AI model service. When empty, safe local normalizers run.
        /// Once populated, operations shift to the AI model.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Base API URL for Meta Model API (default: "https://api.meta.ai/v1").
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.meta.ai/v1";

        /// <summary>
        /// Model identifier for Meta Muse free plan (default: "muse-spark-1.3").
        /// </summary>
        public string Model { get; set; } = "muse-spark-1.3";

        /// <summary>
        /// Minimum confidence threshold (0.0 to 1.0) to consider a match equivalent. Default: 0.70.
        /// </summary>
        public double EquivalenceThreshold { get; set; } = 0.70;

        /// <summary>
        /// When true, enables AI evaluation for drug names, brand-to-generic substitutions, and strengths.
        /// </summary>
        public bool EnableAiDrugEquivalence { get; set; } = true;

        /// <summary>
        /// When true, enables AI evaluation for prescription regulatory and clinical compliance.
        /// </summary>
        public bool EnableAiComplianceAudit { get; set; } = true;

        /// <summary>
        /// HTTP request timeout in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 15;

        /// <summary>
        /// Indicates if an API key is configured and AI is active.
        /// </summary>
        public bool IsAiEnabled => !string.IsNullOrWhiteSpace(ApiKey);
    }
}
