using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class TeacherRepository : BaseRepository<Teacher>, ITeacherRepository
	{
		public TeacherRepository(ManagementDbContext context) : base(context)
		{
		}
	}
}
