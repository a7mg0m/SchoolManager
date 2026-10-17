using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class StudentRepository : BaseRepository<Student>, IStudentRepository
	{
		public StudentRepository(ManagementDbContext context) : base(context)
		{
		}
	}
}
