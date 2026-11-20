using BuildingBlocks;

namespace SchoolManager.Notification.Domain
{
    public interface IChatRoomRepository : IRepository<ChatRoom>
    {
        Task<ChatRoom?> GetByIdWithParticipantsAsync(Guid roomId, CancellationToken cancellationToken = default);
        /// <summary>The private room of two users (of an academic year when given)</summary>
        Task<ChatRoom?> GetPrivateRoomAsync(Guid userA, Guid userB, Guid? academicYearId = null, CancellationToken cancellationToken = default);
        Task<ChatRoom?> GetSectionGroupRoomAsync(Guid sectionId, CancellationToken cancellationToken = default);
        Task<List<ChatRoom>> GetUserRoomsAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
