using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class NotificationTargetRepository : BaseRepository<NotificationTarget>, INotificationTargetRepository
    {
        public NotificationTargetRepository(NotificationDbContext context) : base(context)
        {
        }
    }
}
