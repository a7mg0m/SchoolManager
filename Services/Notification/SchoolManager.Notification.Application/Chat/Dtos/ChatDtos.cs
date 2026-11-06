using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public record AttachmentDto(string FileName, string Url, string ContentType, long SizeInBytes);

    public record ChatParticipantDto(Guid UserId, string? DisplayName);

    public record ChatRoomDto(
        Guid Id,
        ChatRoomType Type,
        string? Name,
        Guid? SectionId,
        IReadOnlyList<ChatParticipantDto> Participants);

    public record ChatMessageDto(
        Guid Id,
        Guid ChatRoomId,
        Guid SenderId,
        string? SenderName,
        string Content,
        DateTime SentOn,
        IReadOnlyList<AttachmentDto> Attachments,
        MessageDeliveryStatus Status);

    public record ChatListItemDto(
        Guid RoomId,
        ChatRoomType Type,
        string Title,
        string? LastMessage,
        Guid? LastMessageSenderId,
        DateTime? LastMessageOn,
        int UnreadCount);

    public record ChatHistoryDto(
        Guid RoomId,
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<ChatMessageDto> Messages);
}
