using Rxintel.Generic.Dtos.Dtos;

namespace Rxintel.Generic.Services.Interfaces
{
    /// <summary>
    /// Validates JWT strings and reads claims from the current HTTP user (minimal surface for Rxintel).
    /// </summary>
    public interface ITokenParser
    {
        Task<TokenVerificationDto> ParseTokenAsync(string token);

        Task<bool> ValidateTokenAsync(string token);

        string? GetClaimValue(string claimType);

        int? GetIntClaim(string claimType);

        Guid? GetGuidClaim(string claimType);

        /// <summary>Current user id from bearer context (<c>sub</c> or name identifier as GUID).</summary>
        Guid? GetUserId();

        string? GetEmail();

        string? GetName();

        IReadOnlyList<string> GetRoles();

        bool IsInRole(string roleName);

        Guid? GetPharmacyId();

        string? GetUserType();

        bool IsEmployee();

        bool IsAdmin();

        /// <summary>Platform super admin (Admin user type with Super Admin or Admin role).</summary>
        bool IsSuperAdmin();
    }
}
