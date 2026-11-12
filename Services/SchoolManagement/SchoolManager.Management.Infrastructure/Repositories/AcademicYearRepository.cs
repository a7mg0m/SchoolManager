using Microsoft.EntityFrameworkCore;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class AcademicYearRepository : BaseRepository<AcademicYear>, IAcademicYearRepository
	{
		public AcademicYearRepository(ManagementDbContext context) : base(context)
		{
		}

		public override Task<AcademicYear?> GetByIdAsync(Guid id)
			=> _dbSet.Include(a => a.Semesters).FirstOrDefaultAsync(a => a.Id == id);

		// Include semesters here too: lists (e.g. school event form) pick a semester from the year
		public override async Task<List<AcademicYear>> GetAllAsync()
			=> await _dbSet.Include(a => a.Semesters).AsNoTracking().ToListAsync();
	}
}
