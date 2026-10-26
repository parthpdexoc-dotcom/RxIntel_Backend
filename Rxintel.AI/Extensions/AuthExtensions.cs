using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Rxintel.Generic.Common.Implementations.Helpers;
using System.Security.Claims;
using System.Text;

namespace Rxintel.AI.Extensions
{
    /// <summary>
    /// JWT Bearer setup aligned with <see cref="Rxintel.Generic.Common.Helpers.TokenHelper"/> and <c>JwtSettings</c>.
    /// </summary>
    public static class AuthExtensions
    {
        public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration configuration)
        {
            var configs = new Configurations(configuration);
            var secretKey = configs.JwtSecretKey;

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = configs.JwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = configs.JwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });

            return services;
        }
    }
}
