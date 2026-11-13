using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks
{
    /// <summary>
    /// Reads the current user from <see cref="HttpContext.User"/>, which the JWT bearer handler fills
    /// after validating the token. Unvalidated input (headers, query string, body) is never trusted.
    /// </summary>
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public Guid UserId =>
            User?.Identity?.IsAuthenticated == true &&
            Guid.TryParse(FindClaim(JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier), out var id)
                ? id
                : Guid.Empty;

        public string? UserName => FindClaim(JwtRegisteredClaimNames.UniqueName, ClaimTypes.Name);

        public string? Email => FindClaim(JwtRegisteredClaimNames.Email, ClaimTypes.Email);

        public bool IsAuthenticated => UserId != Guid.Empty;

        /// <summary>
        /// The JWT handler may keep the short claim name ("sub") or map it to the long
        /// ClaimTypes URI, depending on MapInboundClaims, so both are checked.
        /// </summary>
        private string? FindClaim(params string[] types)
        {
            var user = User;
            if (user is null) return null;

            foreach (var type in types)
            {
                var value = user.FindFirst(type)?.Value;
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            return null;
        }
    }
}
