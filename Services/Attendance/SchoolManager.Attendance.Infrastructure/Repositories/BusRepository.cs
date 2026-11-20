using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class BusRepository : BaseRepository<Bus>, IBusRepository
    {
        public BusRepository(AttendanceDbContext context) : base(context)
        {
        }
    }
}
