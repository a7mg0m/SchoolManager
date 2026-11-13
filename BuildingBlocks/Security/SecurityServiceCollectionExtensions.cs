using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks
{
    public static class SecurityServiceCollectionExtensions
    {
        /// <summary>Registers <see cref="ICurrentUserService"/> (scoped, per request).</summary>
        public static IServiceCollection AddCurrentUser(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            return services;
        }

        /// <summary>
        /// Validates the JWT issued by the Identity service (settings from the "Jwt" section:
        /// Key, Issuer, Audience), so <see cref="ICurrentUserService"/> can read the user from it,
        /// and registers <see cref="ICurrentUserService"/>.
        /// Call app.UseAuthentication() before the endpoints.
        /// </summary>
        public static IServiceCollection AddJwtCurrentUser(this IServiceCollection services, IConfiguration configuration)
        {
            var jwt = configuration.GetSection("Jwt");

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwt["Issuer"],
                        ValidAudience = jwt["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"] ?? string.Empty))
                    };
                });

            return services.AddCurrentUser();
        }
    }
}
