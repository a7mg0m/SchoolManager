using Microsoft.EntityFrameworkCore;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public class EmployeeRepository : BaseRepository<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(IdentityDbContext context) : base(context)
        {
        }

        private IQueryable<Employee> WithGroup => _context.Employees.Include(e => e.EmployeeGroup).Include(e => e.User);

        public Task<List<Employee>> GetAllWithGroupAsync()
            => WithGroup.OrderBy(e => e.FullName.English).ToListAsync();

        public Task<Employee?> GetByIdWithGroupAsync(Guid id)
            => WithGroup.FirstOrDefaultAsync(e => e.Id == id);

        public Task<List<Employee>> GetByGroupIdAsync(Guid groupId)
            => WithGroup.Where(e => e.EmployeeGroupId == groupId)
                .OrderBy(e => e.FullName.English)
                .ToListAsync();

        public Task<List<Employee>> SearchAsync(string searchTerm)
        {
            var term = (searchTerm ?? string.Empty).Trim();
            return WithGroup
                .Where(e => e.FullName.Arabic.Contains(term)
                    || e.FullName.English.Contains(term)
                    || e.NationalId.Contains(term)
                    || e.PhoneNumber.Contains(term)
                    || (e.Email != null && e.Email.Contains(term))
                    || e.JobTitle.Contains(term))
                .OrderBy(e => e.FullName.English)
                .ToListAsync();
        }

        public Task<bool> NationalIdExistsAsync(string nationalId, Guid? excludeId = null)
        {
            var id = nationalId.Trim();
            return _context.Employees.AnyAsync(e => e.NationalId == id && (excludeId == null || e.Id != excludeId));
        }
    }
}
