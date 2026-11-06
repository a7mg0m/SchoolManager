using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    internal static class ChatMappings
    {
        public static AttachmentDto ToDto(this Attachment a) => new(a.FileName, a.Url, a.ContentType, a.SizeInBytes);

        public static Attachment ToDomain(this AttachmentDto a) => new(a.FileName, a.Url, a.ContentType, a.SizeInBytes);

        /// <summary>Sender sees the lowest recipient status; a recipient sees their own status.</summary>
        public static ChatMessageDto ToDto(this ChatMessage m, Guid viewerId, IReadOnlyDictionary<Guid, string> names)
        {
            MessageDeliveryStatus status;
            if (m.SenderId == viewerId)
                status = m.Statuses.Count == 0 ? MessageDeliveryStatus.Sent : m.Statuses.Min(s => s.Status);
            else
                status = m.Statuses.FirstOrDefault(s => s.UserId == viewerId)?.Status ?? MessageDeliveryStatus.Sent;

            return new ChatMessageDto(
                m.Id,
                m.ChatRoomId,
                m.SenderId,
                names.TryGetValue(m.SenderId, out var name) ? name : null,
                m.Content,
                m.SentOn,
                m.Attachments.Select(a => a.ToDto()).ToList(),
                status);
        }
    }
}
