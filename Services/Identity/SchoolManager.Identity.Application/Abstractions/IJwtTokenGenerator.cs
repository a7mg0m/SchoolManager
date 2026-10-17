
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}
