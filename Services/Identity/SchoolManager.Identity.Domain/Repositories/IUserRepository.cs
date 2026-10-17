using BuildingBlocks;

namespace SchoolManager.Identity.Domain
{
    public interface IUserRepository : IRepository<User>
    {
        Task<List<User>> SearchAsync(string searchTerm);
        Task<User?> GetByLoginNameAsync(string loginName);
        Task<List<User>> GetAllNonReservedAsync();
    }
}
