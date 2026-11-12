using BuildingBlocks;

namespace SchoolManager.Notification.Domain
{
    /// <summary>Delivery state of a message for one recipient.</summary>
    public sealed class MessageStatus : ChatEntity
    {
        public Guid MessageId { get; private set; }
        public Guid ChatRoomId { get; private set; }
        public Guid UserId { get; private set; }
        public MessageDeliveryStatus Status { get; private set; }
        public DateTime? DeliveredOn { get; private set; }
        public DateTime? SeenOn { get; private set; }
        public AuditInfo AuditInfo { get; private set; } = null!;

        private MessageStatus() { }

        internal MessageStatus(Guid messageId, Guid chatRoomId, Guid userId, AuditInfo auditInfo)
        {
            MessageId = messageId;
            ChatRoomId = chatRoomId;
            UserId = userId;
            Status = MessageDeliveryStatus.Sent;
            AuditInfo = auditInfo.Copy(); // copy: the message and every recipient status share one instance (owned type)
        }

        public void MarkDelivered(AuditInfo auditInfo)
        {
            if (Status >= MessageDeliveryStatus.Delivered) return;

            Status = MessageDeliveryStatus.Delivered;
            DeliveredOn = auditInfo.CreatedOn;
            Touch(auditInfo);
            Raise(new MessageDeliveredEvent(MessageId, ChatRoomId, UserId, auditInfo.CreatedOn));
        }

        public void MarkSeen(AuditInfo auditInfo)
        {
            if (Status == MessageDeliveryStatus.Seen) return;

            DeliveredOn ??= auditInfo.CreatedOn;
            Status = MessageDeliveryStatus.Seen;
            SeenOn = auditInfo.CreatedOn;
            Touch(auditInfo);
            Raise(new MessageSeenEvent(MessageId, ChatRoomId, UserId, auditInfo.CreatedOn));
        }

        private void Touch(AuditInfo auditInfo) =>
            AuditInfo = new AuditInfo(AuditInfo.CreatedOn, AuditInfo.CreatedBy, auditInfo.CreatedOn, auditInfo.CreatedBy);
    }
}
