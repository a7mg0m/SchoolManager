using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class SemesterRepository : BaseRepository<Semester>, ISemesterRepository
	{
		public SemesterRepository(ManagementDbContext context) : base(context)
		{
		}
	}
}
