using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class TimetableSlotRepository : BaseRepository<TimetableSlot>, ITimetableSlotRepository
	{
		public TimetableSlotRepository(ManagementDbContext context) : base(context)
		{
		}
	}
}
