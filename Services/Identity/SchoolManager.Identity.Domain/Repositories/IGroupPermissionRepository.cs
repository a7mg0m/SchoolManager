using BuildingBlocks;

namespace SchoolManager.Identity.Domain
{
    public interface IGroupPermissionRepository : IRepository<GroupPermission>
    {
        /// <summary>All permission rows of one employee group.</summary>
        Task<List<GroupPermission>> GetByGroupAsync(Guid employeeGroupId);
    }
}
