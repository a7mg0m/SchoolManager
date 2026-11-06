using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
    public record GetChatListQuery(Guid UserId) : IQuery<IReadOnlyList<ChatListItemDto>>;

    public record GetChatHistoryQuery(Guid RoomId, Guid UserId, int Page = 1, int PageSize = 50) : IQuery<ChatHistoryDto>;
}
