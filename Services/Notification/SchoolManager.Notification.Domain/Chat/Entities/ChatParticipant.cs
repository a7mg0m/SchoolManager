using BuildingBlocks;

namespace SchoolManager.Notification.Domain
{
    public sealed class ChatParticipant : BaseEntity
    {
        public Guid ChatRoomId { get; private set; }
        public Guid UserId { get; private set; }
        public DateTime JoinedOn { get; private set; }
        public bool IsActive { get; private set; }
        public AuditInfo AuditInfo { get; private set; } = null!;

        private ChatParticipant() { }

        internal ChatParticipant(Guid chatRoomId, Guid userId, AuditInfo auditInfo)
        {
            if (userId == Guid.Empty) throw new ChatDomainException("UserId is required.");

            ChatRoomId = chatRoomId;
            UserId = userId;
            JoinedOn = auditInfo.CreatedOn;
            IsActive = true;
            AuditInfo = auditInfo;
        }

        internal void Rejoin(AuditInfo auditInfo)
        {
            IsActive = true;
            AuditInfo = new AuditInfo(AuditInfo.CreatedOn, AuditInfo.CreatedBy, auditInfo.CreatedOn, auditInfo.CreatedBy);
        }

        internal void Leave(AuditInfo auditInfo)
        {
            IsActive = false;
            AuditInfo = new AuditInfo(AuditInfo.CreatedOn, AuditInfo.CreatedBy, auditInfo.CreatedOn, auditInfo.CreatedBy);
        }
    }
}
