namespace Rxintel.Generic.Dtos.Dtos
{
    /// <summary>
    /// Result of validating and decoding a JWT.
    /// </summary>
    public class TokenVerificationDto
    {
        public bool IsValid { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public DateTime? IssuedAt { get; set; }

        public string? Issuer { get; set; }

        public string? Audience { get; set; }

        /// <summary>All distinct claim types merged into a single value per type (last wins if duplicated).</summary>
        public Dictionary<string, string> Claims { get; set; } = new();

        /// <summary>All role claim values (<see cref="System.Security.Claims.ClaimTypes.Role"/>).</summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>User id from <c>sub</c> or <see cref="System.Security.Claims.ClaimTypes.NameIdentifier"/> when it is a GUID.</summary>
        public Guid? UserId { get; set; }

        public string? Email { get; set; }

        public string? Name { get; set; }

        public Guid? PharmacyId { get; set; }

        public string? UserType { get; set; }
    }
}
