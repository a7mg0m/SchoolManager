using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class AcademicYearRepository : BaseRepository<AcademicYear>, IAcademicYearRepository
	{
		public AcademicYearRepository(ManagementDbContext context) : base(context)
		{
		}
	}
}
