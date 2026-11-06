using BuildingBlocks;

namespace SchoolManager.Notification.Domain
{
    public interface IChatRoomRepository : IRepository<ChatRoom>
    {
        Task<ChatRoom?> GetByIdWithParticipantsAsync(Guid roomId, CancellationToken cancellationToken = default);
        Task<ChatRoom?> GetPrivateRoomAsync(Guid userA, Guid userB, CancellationToken cancellationToken = default);
        Task<ChatRoom?> GetSectionGroupRoomAsync(Guid sectionId, CancellationToken cancellationToken = default);
        Task<List<ChatRoom>> GetUserRoomsAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
