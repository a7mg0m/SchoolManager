using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class EventAttendanceRepository : BaseRepository<EventAttendance>, IEventAttendanceRepository
    {
        public EventAttendanceRepository(NotificationDbContext context) : base(context)
        {
        }
    }
}
