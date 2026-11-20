using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <param name="AcademicYearId">Year of the chat (teacher / parent apps); null = a chat without a year</param>
    public record CreatePrivateChatCommand(Guid CurrentUserId, Guid OtherUserId, Guid? AcademicYearId = null) : ICommand<Guid>;

    public record CreateGroupChatCommand(Guid TeacherId, Guid SectionId, string Name, IReadOnlyCollection<Guid> ParentIds) : ICommand<Guid>;

    public record SendMessageCommand(
        Guid RoomId,
        Guid SenderId,
        string Content,
        IReadOnlyCollection<AttachmentDto>? Attachments) : ICommand<ChatMessageDto>;

    public record UpdateMessageStatusCommand(
        Guid RoomId,
        Guid UserId,
        IReadOnlyCollection<Guid> MessageIds,
        MessageDeliveryStatus Status) : ICommand;
}
