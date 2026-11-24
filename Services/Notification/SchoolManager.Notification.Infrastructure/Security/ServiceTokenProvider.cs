using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SchoolManager.Notification.Infrastructure
{
    /// <summary>
    /// A short-lived token for calls this service makes on its own (e.g. the background push job), signed with the
    /// same Jwt settings as the Identity service, so the other services accept it. It carries no parent / teacher /
    /// driver claim, only "svc" = "notification".
    /// </summary>
    public class ServiceTokenProvider(IConfiguration configuration)
    {
        public const string ServiceClaim = "svc";
        public const string ServiceName = "notification";

        private string? _token;
        private DateTime _expiresAt;
        private readonly object _lock = new();

        public string GetToken()
        {
            lock (_lock)
            {
                if (_token is not null && DateTime.UtcNow < _expiresAt.AddMinutes(-1))
                    return _token;

                var jwt = configuration.GetSection("Jwt");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"] ?? string.Empty));
                _expiresAt = DateTime.UtcNow.AddMinutes(10);
                _token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
                {
                    Issuer = jwt["Issuer"],
                    Audience = jwt["Audience"],
                    Expires = _expiresAt,
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString()),
                        new Claim(ServiceClaim, ServiceName)
                    }),
                    SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
                });
                return _token;
            }
        }
    }
}
