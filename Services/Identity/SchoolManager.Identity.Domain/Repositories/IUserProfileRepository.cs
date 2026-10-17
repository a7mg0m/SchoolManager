using BuildingBlocks;

namespace SchoolManager.Identity.Domain
{
    public interface IUserProfileRepository : IRepository<UserProfile>
    {
        Task<List<UserProfile>> SearchAsync(string searchTerm);
    }
}
