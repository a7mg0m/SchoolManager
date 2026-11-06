using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.API
{
    public record SendMessageRequest(Guid RoomId, string? Content, List<AttachmentDto>? Attachments);

    public record CreatePrivateChatRequest(Guid OtherUserId);

    public record CreateGroupChatRequest(Guid SectionId, string Name, List<Guid> ParentIds);

    public record UpdateMessageStatusRequest(List<Guid> MessageIds, SchoolManager.Notification.Domain.MessageDeliveryStatus Status);
}
