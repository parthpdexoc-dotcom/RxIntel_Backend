using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Rxintel.Generic.Common.Constants;
using Rxintel.Generic.Common.Enums;
using Rxintel.Generic.Common.Implementations.Helpers;
using Rxintel.Generic.Entities.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Rxintel.Generic.Common.Helpers
{
    /// <summary>
    /// JWT generation for admin and pharmacy user (employee) logins.
    /// </summary>
    public static class TokenHelper
    {
        private const string BearerAuthenticationType = "Bearer";

        public static int GetTokenExpirationInMinutes(IConfiguration configuration) =>
            GetTokenExpirationInMinutes(new Configurations(configuration));

        public static int GetTokenExpirationInMinutes(Configurations configurations)
        {
            ArgumentNullException.ThrowIfNull(configurations);
            return configurations.JwtExpirationInMinutes;
        }

        public static string GenerateAdminToken(
            IConfiguration configuration,
            Guid adminId,
            string userName,
            string? name,
            string? email,
            string? role) =>
            GenerateAdminToken(new Configurations(configuration), adminId, userName, name, email, role);

        public static string GenerateAdminToken(
            Configurations configurations,
            Guid adminId,
            string userName,
            string? name,
            string? email,
            string? role)
        {
            var displayName = string.IsNullOrWhiteSpace(name) ? userName : name!;
            var roleValue = !string.IsNullOrWhiteSpace(role) ? role.Trim() : RoleTypeName.Admin;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, displayName),
                new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, adminId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),

                new Claim(ClaimNames.UserType, UserType.Admin.ToString()),
                new Claim(ClaimNames.AdminId, adminId.ToString()),
                new Claim(ClaimNames.Username, userName),
                new Claim(ClaimNames.FullName, displayName),
                new Claim(ClaimNames.Role, roleValue),
                new Claim("role", roleValue),
                new Claim(ClaimTypes.Role, roleValue)
            };

            if (!string.IsNullOrWhiteSpace(email))
                claims.Add(new Claim(ClaimTypes.Email, email));

            return SignToken(configurations, claims);
        }

        public static string GenerateEmployeeToken(
            IConfiguration configuration,
            PharmacyUser user) =>
            GenerateEmployeeToken(new Configurations(configuration), user);

        public static string GenerateEmployeeToken(
            Configurations configurations,
            PharmacyUser user)
        {
            var role = user.Role.HasValue
                ? EnumHelper.ToStringValue(user.Role.Value)
                : string.Empty;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),

                new Claim(ClaimNames.UserType, UserType.Employee.ToString()),
                new Claim(ClaimNames.PharmacyUserId, user.Id.ToString()),
                new Claim(ClaimNames.PharmacyId, user.PharmacyId.ToString()),
                new Claim(ClaimNames.Username, user.UserEmail),
                new Claim(ClaimNames.FullName, user.Name),
                new Claim(ClaimNames.IsActive, user.IsActive.ToString()),
                new Claim(ClaimTypes.Email, user.UserEmail),
            };

            if (!string.IsNullOrWhiteSpace(role))
            {
                claims.Add(new Claim(ClaimNames.Role, role));
                claims.Add(new Claim("role", role));
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return SignToken(configurations, claims);
        }

        private static string SignToken(Configurations configurations, List<Claim> claims)
        {
            var secretKey = configurations.JwtSecretKey;
            if (secretKey.Length < 32)
                throw new InvalidOperationException("JwtSettings:SecretKey must be at least 32 characters for HS256.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var now = DateTime.UtcNow;
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims, BearerAuthenticationType),
                Issuer = configurations.JwtIssuer,
                Audience = configurations.JwtAudience,
                Expires = now.AddMinutes(configurations.JwtExpirationInMinutes),
                IssuedAt = now,
                SigningCredentials = creds
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }
    }
}
