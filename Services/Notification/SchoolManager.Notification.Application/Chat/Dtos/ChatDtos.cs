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
        int UnreadCount,
        /// <summary>The other person of a private chat</summary>
        Guid? OtherUserId = null,
        /// <summary>What the last message carries when it has no text: "image" | "video" | "audio" | "file" (null: text)</summary>
        string? LastAttachmentKind = null);

    /// <summary>A private chat of the logged-in user (teacher / parent apps), with the other person's names.</summary>
    public record MyChatDto(
        Guid RoomId,
        Guid OtherUserId,
        string NameAr,
        string NameEn,
        string DetailAr,
        string DetailEn,
        string? LastMessage,
        Guid? LastMessageSenderId,
        DateTime? LastMessageOn,
        int UnreadCount,
        /// <summary>"image" | "video" | "audio" | "file" when the last message is a picture / video / voice note (the app shows its own label)</summary>
        string? LastAttachmentKind = null);

    public record ChatHistoryDto(
        Guid RoomId,
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<ChatMessageDto> Messages);
}
