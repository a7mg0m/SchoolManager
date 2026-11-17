
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public interface IJwtTokenGenerator
    {
        /// <summary>
        /// Token for the user. When a profile is given, its linked employee / teacher / parent ids
        /// are added as claims (<see cref="BuildingBlocks.AppClaimTypes"/>).
        /// </summary>
        string GenerateToken(User user, UserProfile? profile = null);
    }
}
