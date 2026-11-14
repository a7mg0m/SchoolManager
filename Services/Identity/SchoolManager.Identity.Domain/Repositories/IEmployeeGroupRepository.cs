using BuildingBlocks;

namespace SchoolManager.Identity.Domain
{
    public interface IEmployeeGroupRepository : IRepository<EmployeeGroup>
    {
        /// <summary>Number of employees in each group, keyed by group id (groups without employees are absent).</summary>
        Task<Dictionary<Guid, int>> GetEmployeeCountsAsync();

        Task<bool> HasEmployeesAsync(Guid groupId);

        /// <summary>True if another group already uses this Arabic or English name.</summary>
        Task<bool> NameExistsAsync(string nameAr, string nameEn, Guid? excludeId = null);
    }
}
