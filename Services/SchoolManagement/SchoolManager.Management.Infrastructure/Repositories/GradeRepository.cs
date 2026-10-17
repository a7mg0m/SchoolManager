using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class GradeRepository : BaseRepository<Grade>, IGradeRepository
	{
		public GradeRepository(ManagementDbContext context) : base(context)
		{
		}
	}
}
