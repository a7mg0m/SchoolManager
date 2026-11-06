using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.API
{
    public record SendMessageResponse(
        Guid MessageId,
        Guid RoomId,
        Guid SenderId,
        string? SenderName,
        string Content,
        DateTime SentOn,
        MessageDeliveryStatus Status);

    public record CreateChatResponse(Guid RoomId);
}
