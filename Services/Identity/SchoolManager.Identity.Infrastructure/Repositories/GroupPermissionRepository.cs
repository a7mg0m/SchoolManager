using Microsoft.EntityFrameworkCore;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public class GroupPermissionRepository : BaseRepository<GroupPermission>, IGroupPermissionRepository
    {
        public GroupPermissionRepository(IdentityDbContext context) : base(context)
        {
        }

        public Task<List<GroupPermission>> GetByGroupAsync(Guid employeeGroupId)
            => _context.GroupPermissions
                .Where(p => p.EmployeeGroupId == employeeGroupId)
                .ToListAsync();
    }
}
