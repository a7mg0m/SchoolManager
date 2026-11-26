using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SchoolManager.Identity.Infrastructure
{
    /// <summary>
    /// A short-lived token for Identity's own calls to Management (sign-in checks), signed with the same Jwt settings:
    /// no parent / teacher / driver claim, "svc" = "identity".
    /// </summary>
    internal static class ManagementServiceToken
    {
        public static string Create(IConfiguration configuration)
        {
            var jwt = configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"] ?? string.Empty));
            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, Guid.Empty.ToString()), new Claim("svc", "identity") },
                expires: DateTime.UtcNow.AddMinutes(2),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
