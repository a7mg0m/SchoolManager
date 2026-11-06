using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class GetChatHistoryQueryHandler(
        IChatRoomRepository rooms,
        IChatMessageRepository messages,
        IUserProfileService userProfiles)
        : IQueryHandler<GetChatHistoryQuery, ChatHistoryDto>
    {
        public async Task<ChatHistoryDto> Handle(GetChatHistoryQuery query, CancellationToken cancellationToken)
        {
            var room = await rooms.GetByIdWithParticipantsAsync(query.RoomId, cancellationToken)
                ?? throw new KeyNotFoundException($"Chat room {query.RoomId} not found");

            if (!room.HasActiveParticipant(query.UserId))
                throw new UnauthorizedAccessException("User is not a participant of this chat room.");

            var items = await messages.GetHistoryAsync(query.RoomId, query.Page, query.PageSize, cancellationToken);
            var total = await messages.CountAsync(query.RoomId, cancellationToken);
            var names = await userProfiles.GetDisplayNamesAsync(items.Select(m => m.SenderId).Distinct(), cancellationToken);

            return new ChatHistoryDto(
                query.RoomId,
                query.Page,
                query.PageSize,
                total,
                items.Select(m => m.ToDto(query.UserId, names)).ToList());
        }
    }
}
