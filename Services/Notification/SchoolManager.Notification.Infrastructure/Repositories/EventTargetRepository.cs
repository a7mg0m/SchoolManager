using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class EventTargetRepository : BaseRepository<EventTarget>, IEventTargetRepository
    {
        public EventTargetRepository(NotificationDbContext context) : base(context)
        {
        }
    }
}
