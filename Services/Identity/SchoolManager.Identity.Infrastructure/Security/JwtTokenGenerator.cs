using BuildingBlocks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchoolManager.Identity.Application;
using SchoolManager.Identity.Domain;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SchoolManager.Identity.Infrastructure
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _settings;

        public JwtTokenGenerator(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public string GenerateToken(User user, UserProfile? profile = null)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.LoginName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Who the user is in the school (read by ICurrentUserService in every service)
            if (profile?.EmployeeId is Guid employeeId)
                claims.Add(new Claim(AppClaimTypes.EmployeeId, employeeId.ToString()));
            if (profile?.TeacherId is Guid teacherId)
                claims.Add(new Claim(AppClaimTypes.TeacherId, teacherId.ToString()));
            if (profile?.ParentId is Guid parentId)
                claims.Add(new Claim(AppClaimTypes.ParentId, parentId.ToString()));
            if (profile?.BusId is Guid busId)
                claims.Add(new Claim(AppClaimTypes.BusId, busId.ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                // App users stay signed in (no practical expiry; checked when the app opens); the dashboard's tokens stay short
                expires: profile?.ParentId is not null || profile?.TeacherId is not null || profile?.BusId is not null
                    ? DateTime.UtcNow.AddDays(_settings.AppExpiryDays)
                    : DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
