using BuildingBlocks;

namespace SchoolManager.Identity.Domain
{
    public interface IEmployeeRepository : IRepository<Employee>
    {
        /// <summary>All employees with their group loaded.</summary>
        Task<List<Employee>> GetAllWithGroupAsync();

        Task<Employee?> GetByIdWithGroupAsync(Guid id);

        Task<List<Employee>> GetByGroupIdAsync(Guid groupId);

        Task<List<Employee>> SearchAsync(string searchTerm);

        Task<bool> NationalIdExistsAsync(string nationalId, Guid? excludeId = null);
    }
}
