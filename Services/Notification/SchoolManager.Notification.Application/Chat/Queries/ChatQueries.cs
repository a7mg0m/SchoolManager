using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
    /// <param name="AcademicYearId">Only the chats of this academic year (null: all)</param>
    /// <param name="ResolveNames">False when the caller has the names already (the apps' contacts): Identity is not asked</param>
    public record GetChatListQuery(Guid UserId, Guid? AcademicYearId = null, bool ResolveNames = true) : IQuery<IReadOnlyList<ChatListItemDto>>;

    public record GetChatHistoryQuery(Guid RoomId, Guid UserId, int Page = 1, int PageSize = 50) : IQuery<ChatHistoryDto>;

    /// <summary>True when the user is an active participant of the room (chat file upload / download)</summary>
    public record IsChatParticipantQuery(Guid RoomId, Guid UserId) : IQuery<bool>;
}
