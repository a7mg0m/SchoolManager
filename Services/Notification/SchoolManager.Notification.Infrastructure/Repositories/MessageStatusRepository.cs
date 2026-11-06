using Microsoft.EntityFrameworkCore;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class MessageStatusRepository(NotificationDbContext context)
        : BaseRepository<MessageStatus>(context), IMessageStatusRepository
    {
        public Task<List<MessageStatus>> GetForUserAsync(Guid roomId, Guid userId, IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default)
        {
            var ids = messageIds.ToList();
            return _dbSet
                .Where(s => s.ChatRoomId == roomId && s.UserId == userId && ids.Contains(s.MessageId))
                .ToListAsync(cancellationToken);
        }
    }
}
