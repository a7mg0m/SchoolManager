using BuildingBlocks;

namespace SchoolManager.Notification.Domain
{
    public interface IMessageStatusRepository : IRepository<MessageStatus>
    {
        Task<List<MessageStatus>> GetForUserAsync(Guid roomId, Guid userId, IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default);
    }
}
