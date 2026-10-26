using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Rxintel.Generic.Common.Constants;
using Rxintel.Generic.Common.Enums;
using Rxintel.Generic.Common.Implementations.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Rxintel.Generic.Services.Services
{
    /// <summary>
    /// Validates and decodes JWTs using the same <c>JwtSettings</c> as <see cref="Rxintel.Generic.Common.Helpers.TokenHelper"/>.
    /// </summary>
    public class TokenParser : ITokenParser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TokenParser> _logger;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;

        public TokenParser(
            Configurations configurations,
            IHttpContextAccessor httpContextAccessor,
            ILogger<TokenParser> logger)
        {
            ArgumentNullException.ThrowIfNull(configurations);
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _secretKey = configurations.JwtSecretKey;
            _issuer = configurations.JwtIssuer;
            _audience = configurations.JwtAudience;
        }

        public async Task<TokenVerificationDto> ParseTokenAsync(string token)
        {
            var result = new TokenVerificationDto();

            if (string.IsNullOrWhiteSpace(token))
            {
                result.IsValid = false;
                result.ErrorMessage = "Token is required.";
                return await Task.FromResult(result);
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    result.IsValid = true;
                    result.Issuer = jwtToken.Issuer;
                    result.Audience = string.Join(",", jwtToken.Audiences);
                    if (jwtToken.ValidTo != DateTime.MinValue)
                        result.ExpiresAt = jwtToken.ValidTo;
                    if (jwtToken.ValidFrom != DateTime.MinValue)
                        result.IssuedAt = jwtToken.ValidFrom;

                    foreach (var claim in jwtToken.Claims)
                        result.Claims[claim.Type] = claim.Value;

                    result.Roles = jwtToken.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToList();

                    result.UserId = GetGuidClaim(jwtToken, JwtRegisteredClaimNames.Sub)
                        ?? GetGuidClaim(jwtToken, ClaimTypes.NameIdentifier);
                    result.Email = GetClaimValue(jwtToken, ClaimTypes.Email);
                    result.Name = GetClaimValue(jwtToken, ClaimTypes.Name);
                    result.PharmacyId = GetGuidClaim(jwtToken, ClaimNames.PharmacyId);
                    result.UserType = GetClaimValue(jwtToken, ClaimNames.UserType);
                }
            }
            catch (SecurityTokenExpiredException)
            {
                result.IsValid = false;
                result.ErrorMessage = "Token has expired.";
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                result.IsValid = false;
                result.ErrorMessage = "Token signature is invalid.";
            }
            catch (SecurityTokenException ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Token validation failed: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Error decoding token: {ex.Message}";
                _logger.LogWarning(ex, "Error parsing token");
            }

            return await Task.FromResult(result);
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out _);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return await Task.FromResult(false);
            }
        }

        public string? GetClaimValue(string claimType)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(claimType)?.Value;
        }

        public int? GetIntClaim(string claimType)
        {
            var value = GetClaimValue(claimType);
            return int.TryParse(value, out var n) ? n : null;
        }

        public Guid? GetGuidClaim(string claimType)
        {
            var value = GetClaimValue(claimType);
            return Guid.TryParse(value, out var g) ? g : null;
        }

        public Guid? GetUserId()
        {
            return GetGuidClaim(JwtRegisteredClaimNames.Sub)
                ?? GetGuidClaim(ClaimTypes.NameIdentifier);
        }

        public string? GetEmail() => GetClaimValue(ClaimTypes.Email);

        public string? GetName() => GetClaimValue(ClaimTypes.Name);

        public IReadOnlyList<string> GetRoles()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                return Array.Empty<string>();

            return user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        }

        public bool IsInRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            var user = _httpContextAccessor.HttpContext?.User;
            return user?.IsInRole(roleName) == true;
        }

        public Guid? GetPharmacyId() => GetGuidClaim(ClaimNames.PharmacyId);

        public string? GetUserType() => GetClaimValue(ClaimNames.UserType);

        public bool IsEmployee() =>
            string.Equals(GetUserType(), UserType.Employee.ToString(), StringComparison.OrdinalIgnoreCase);

        public bool IsAdmin() =>
            string.Equals(GetUserType(), UserType.Admin.ToString(), StringComparison.OrdinalIgnoreCase);

        public bool IsSuperAdmin()
        {
            if (!IsAdmin())
                return false;

            var roles = GetRoles();
            if (roles.Count == 0)
                return true;

            foreach (var role in roles)
            {
                if (string.IsNullOrWhiteSpace(role))
                    continue;

                if (string.Equals(role, RoleTypeName.SuperAdmin, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(role, RoleTypeName.Admin, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string? GetClaimValue(JwtSecurityToken token, string claimType) =>
            token.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;

        private static Guid? GetGuidClaim(JwtSecurityToken token, string claimType)
        {
            var value = GetClaimValue(token, claimType);
            return Guid.TryParse(value, out var g) ? g : null;
        }
    }
}
