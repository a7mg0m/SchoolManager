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
	}
}
