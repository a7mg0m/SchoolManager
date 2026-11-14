using Microsoft.EntityFrameworkCore;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public class EmployeeGroupRepository : BaseRepository<EmployeeGroup>, IEmployeeGroupRepository
    {
        public EmployeeGroupRepository(IdentityDbContext context) : base(context)
        {
        }

        public async Task<Dictionary<Guid, int>> GetEmployeeCountsAsync()
        {
            return await _context.Employees
                .GroupBy(e => e.EmployeeGroupId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
        }

        public Task<bool> HasEmployeesAsync(Guid groupId)
            => _context.Employees.AnyAsync(e => e.EmployeeGroupId == groupId);

        public Task<bool> NameExistsAsync(string nameAr, string nameEn, Guid? excludeId = null)
        {
            var ar = nameAr.Trim();
            var en = nameEn.Trim();
            return _context.EmployeeGroups.AnyAsync(g =>
                (excludeId == null || g.Id != excludeId) &&
                (g.Name.Arabic == ar || g.Name.English == en));
        }
    }
}
