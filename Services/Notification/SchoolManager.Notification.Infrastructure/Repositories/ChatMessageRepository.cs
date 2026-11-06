using Microsoft.EntityFrameworkCore;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class ChatMessageRepository(NotificationDbContext context)
        : BaseRepository<ChatMessage>(context), IChatMessageRepository
    {
        public Task<List<ChatMessage>> GetHistoryAsync(Guid roomId, int page, int pageSize, CancellationToken cancellationToken = default) =>
            _dbSet.AsNoTracking()
                .Include(m => m.Statuses)
                .AsSplitQuery()
                .Where(m => m.ChatRoomId == roomId)
                .OrderByDescending(m => m.SentOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        public Task<int> CountAsync(Guid roomId, CancellationToken cancellationToken = default) =>
            _dbSet.CountAsync(m => m.ChatRoomId == roomId, cancellationToken);

        public async Task<Dictionary<Guid, ChatMessage>> GetLastMessagesAsync(IEnumerable<Guid> roomIds, CancellationToken cancellationToken = default)
        {
            var ids = roomIds.ToList();
            var latest = await _dbSet.AsNoTracking()
                .Where(m => ids.Contains(m.ChatRoomId))
                .Where(m => m.SentOn == _dbSet.Where(x => x.ChatRoomId == m.ChatRoomId).Max(x => x.SentOn))
                .ToListAsync(cancellationToken);

            return latest.GroupBy(m => m.ChatRoomId).ToDictionary(g => g.Key, g => g.First());
        }

        public async Task<Dictionary<Guid, int>> GetUnreadCountsAsync(Guid userId, IEnumerable<Guid> roomIds, CancellationToken cancellationToken = default)
        {
            var ids = roomIds.ToList();
            return await _context.Set<MessageStatus>().AsNoTracking()
                .Where(s => s.UserId == userId && s.Status != MessageDeliveryStatus.Seen && ids.Contains(s.ChatRoomId))
                .GroupBy(s => s.ChatRoomId)
                .Select(g => new { RoomId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoomId, x => x.Count, cancellationToken);
        }
    }
}
