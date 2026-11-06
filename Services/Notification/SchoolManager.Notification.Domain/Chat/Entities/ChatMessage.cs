using BuildingBlocks;

namespace SchoolManager.Notification.Domain
{
    public sealed class ChatMessage : ChatEntity
    {
        public const int MaxContentLength = 4000;

        private readonly List<Attachment> _attachments = new();
        private readonly List<MessageStatus> _statuses = new();

        public Guid ChatRoomId { get; private set; }
        public Guid SenderId { get; private set; }
        public string Content { get; private set; } = string.Empty;
        public DateTime SentOn { get; private set; }
        public AuditInfo AuditInfo { get; private set; } = null!;

        public IReadOnlyCollection<Attachment> Attachments => _attachments.AsReadOnly();
        public IReadOnlyCollection<MessageStatus> Statuses => _statuses.AsReadOnly();

        private ChatMessage() { }

        internal static ChatMessage Create(
            Guid chatRoomId,
            Guid senderId,
            string content,
            IEnumerable<Attachment>? attachments,
            IReadOnlyCollection<Guid> recipientIds,
            AuditInfo auditInfo)
        {
            var attachmentList = attachments?.ToList() ?? new List<Attachment>();
            var text = content?.Trim() ?? string.Empty;

            if (text.Length == 0 && attachmentList.Count == 0)
                throw new ChatDomainException("A message must contain text or at least one attachment.");
            if (text.Length > MaxContentLength)
                throw new ChatDomainException($"Message content cannot exceed {MaxContentLength} characters.");

            var message = new ChatMessage
            {
                ChatRoomId = chatRoomId,
                SenderId = senderId,
                Content = text,
                SentOn = auditInfo.CreatedOn,
                AuditInfo = auditInfo
            };
            message._attachments.AddRange(attachmentList);

            foreach (var recipientId in recipientIds)
                message._statuses.Add(new MessageStatus(message.Id, message.ChatRoomId, recipientId, auditInfo));

            message.Raise(new MessageSentEvent(message.Id, chatRoomId, senderId, recipientIds, auditInfo.CreatedOn));
            return message;
        }
    }
}
