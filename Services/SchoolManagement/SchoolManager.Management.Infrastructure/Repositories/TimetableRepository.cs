using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class TimetableRepository : BaseRepository<Timetable>, ITimetableRepository
	{
		public TimetableRepository(ManagementDbContext context) : base(context)
		{
		}
	}
}
