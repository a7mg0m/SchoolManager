using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public record CreatePrivateChatCommand(Guid CurrentUserId, Guid OtherUserId) : ICommand<Guid>;

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
