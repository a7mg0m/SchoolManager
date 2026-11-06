using Microsoft.EntityFrameworkCore;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class ChatRoomRepository(NotificationDbContext context)
        : BaseRepository<ChatRoom>(context), IChatRoomRepository
    {
        public override Task<ChatRoom?> GetByIdAsync(Guid id) =>
            _dbSet.Include(r => r.Participants).FirstOrDefaultAsync(r => r.Id == id);

        public Task<ChatRoom?> GetByIdWithParticipantsAsync(Guid roomId, CancellationToken cancellationToken = default) =>
            _dbSet.Include(r => r.Participants).FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);

        public Task<ChatRoom?> GetPrivateRoomAsync(Guid userA, Guid userB, CancellationToken cancellationToken = default)
        {
            var key = ChatRoom.BuildPrivateKey(userA, userB);
            return _dbSet.Include(r => r.Participants).FirstOrDefaultAsync(r => r.PrivateKey == key, cancellationToken);
        }

        public Task<ChatRoom?> GetSectionGroupRoomAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            _dbSet.Include(r => r.Participants).FirstOrDefaultAsync(r => r.SectionId == sectionId, cancellationToken);

        public Task<List<ChatRoom>> GetUserRoomsAsync(Guid userId, CancellationToken cancellationToken = default) =>
            _dbSet.Include(r => r.Participants)
                .Where(r => r.Participants.Any(p => p.UserId == userId && p.IsActive))
                .ToListAsync(cancellationToken);
    }
}
