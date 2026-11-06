using BuildingBlocks;

namespace SchoolManager.Notification.Domain
{
    public interface IChatMessageRepository : IRepository<ChatMessage>
    {
        /// <summary>Returns messages (with statuses) newest first.</summary>
        Task<List<ChatMessage>> GetHistoryAsync(Guid roomId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<int> CountAsync(Guid roomId, CancellationToken cancellationToken = default);
        Task<Dictionary<Guid, ChatMessage>> GetLastMessagesAsync(IEnumerable<Guid> roomIds, CancellationToken cancellationToken = default);
        Task<Dictionary<Guid, int>> GetUnreadCountsAsync(Guid userId, IEnumerable<Guid> roomIds, CancellationToken cancellationToken = default);
    }
}
