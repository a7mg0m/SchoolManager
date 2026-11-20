using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
    /// <param name="AcademicYearId">Only the chats of this academic year (null: all)</param>
    public record GetChatListQuery(Guid UserId, Guid? AcademicYearId = null) : IQuery<IReadOnlyList<ChatListItemDto>>;

    public record GetChatHistoryQuery(Guid RoomId, Guid UserId, int Page = 1, int PageSize = 50) : IQuery<ChatHistoryDto>;
}
