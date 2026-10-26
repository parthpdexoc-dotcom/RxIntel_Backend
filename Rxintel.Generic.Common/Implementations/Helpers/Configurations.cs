using System.Globalization;
using Microsoft.Extensions.Configuration;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Models;

namespace Rxintel.Generic.Common.Implementations.Helpers
{
    /// <summary>
    /// Typed configuration facade over existing appsettings keys (OrigencePart-aligned).
    /// Does not rename keys; uses bounded parsers and safe defaults.
    /// </summary>
    public class Configurations
    {
        public Configurations(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IConfiguration Configuration { get; }

        private string? GetConfigurationValue(string key) => Configuration[key];

        private static int ParseBoundedInt(string? raw, int fallback, int min, int max)
        {
            if (string.IsNullOrWhiteSpace(raw)
                || !int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                return fallback;
            return Math.Clamp(n, min, max);
        }

        private static long ParseBoundedLong(string? raw, long fallback, long min, long max)
        {
            if (string.IsNullOrWhiteSpace(raw)
                || !long.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                return fallback;
            return Math.Clamp(n, min, max);
        }

        #region JwtSettings

        public string JwtSecretKey =>
            GetConfigurationValue("JwtSettings:SecretKey")
            ?? throw new InvalidOperationException("JWT signing key is not configured (JwtSettings:SecretKey).");

        public string JwtIssuer => GetConfigurationValue("JwtSettings:Issuer") ?? "RxIntelApi";

        public string JwtAudience => GetConfigurationValue("JwtSettings:Audience") ?? "RxIntelUser";

        public int JwtExpirationInMinutes =>
            ParseBoundedInt(GetConfigurationValue("JwtSettings:ExpirationInMinutes"), 60, 1, 24 * 60);

        #endregion

        #region PharmacyDocumentStorage

        public long PharmacyDocumentMaxFileSizeBytes =>
            ParseBoundedLong(
                GetConfigurationValue("PharmacyDocumentStorage:MaxFileSizeBytes"),
                PharmacyDocumentStorageHelper.DefaultMaxFileSizeBytes,
                1,
                100L * 1024 * 1024);

        #endregion

        #region EmailSettings

        public EmailSettings GetEmailSettings() => EmailSettingsResolver.Resolve(Configuration);

        #endregion
    }
}
