using BuildingBlocks;

namespace SchoolManager.Notification.Domain
{
    public sealed class ChatRoom : ChatEntity
    {
        private readonly List<ChatParticipant> _participants = new();

        public ChatRoomType Type { get; private set; }
        public string? Name { get; private set; }
        public Guid? SectionId { get; private set; }

        /// <summary>Deterministic key (sorted user ids) that guarantees one private room per user pair.</summary>
        public string? PrivateKey { get; private set; }

        public DateTime? LastMessageOn { get; private set; }
        public AuditInfo AuditInfo { get; private set; } = null!;

        public IReadOnlyCollection<ChatParticipant> Participants => _participants.AsReadOnly();

        private ChatRoom() { }

        public static string BuildPrivateKey(Guid userA, Guid userB)
        {
            var ordered = userA.CompareTo(userB) <= 0 ? (userA, userB) : (userB, userA);
            return $"{ordered.Item1:N}_{ordered.Item2:N}";
        }

        public static ChatRoom CreatePrivate(Guid userA, Guid userB, AuditInfo auditInfo)
        {
            if (userA == Guid.Empty || userB == Guid.Empty) throw new ChatDomainException("Both users are required.");
            if (userA == userB) throw new ChatDomainException("Cannot create a private chat with yourself.");

            var room = new ChatRoom
            {
                Type = ChatRoomType.Private,
                PrivateKey = BuildPrivateKey(userA, userB),
                AuditInfo = auditInfo
            };
            room.AddParticipant(userA, auditInfo);
            room.AddParticipant(userB, auditInfo);
            return room;
        }

        public static ChatRoom CreateSectionGroup(
            Guid sectionId, string name, Guid teacherId, IEnumerable<Guid> parentIds, AuditInfo auditInfo)
        {
            if (sectionId == Guid.Empty) throw new ChatDomainException("SectionId is required.");
            if (string.IsNullOrWhiteSpace(name)) throw new ChatDomainException("Group name is required.");
            if (teacherId == Guid.Empty) throw new ChatDomainException("TeacherId is required.");

            var room = new ChatRoom
            {
                Type = ChatRoomType.SectionGroup,
                Name = name.Trim(),
                SectionId = sectionId,
                AuditInfo = auditInfo
            };
            room.AddParticipant(teacherId, auditInfo);
            foreach (var parentId in parentIds) room.AddParticipant(parentId, auditInfo);
            return room;
        }

        public void AddParticipant(Guid userId, AuditInfo auditInfo)
        {
            var existing = _participants.FirstOrDefault(p => p.UserId == userId);
            if (existing is null)
                _participants.Add(new ChatParticipant(Id, userId, auditInfo));
            else if (!existing.IsActive)
                existing.Rejoin(auditInfo);
        }

        public void RemoveParticipant(Guid userId, AuditInfo auditInfo)
        {
            if (Type == ChatRoomType.Private) throw new ChatDomainException("Participants of a private chat cannot be removed.");
            _participants.FirstOrDefault(p => p.UserId == userId && p.IsActive)?.Leave(auditInfo);
        }

        public bool HasActiveParticipant(Guid userId) => _participants.Any(p => p.UserId == userId && p.IsActive);

        public IReadOnlyCollection<Guid> GetRecipientIds(Guid senderId) =>
            _participants.Where(p => p.IsActive && p.UserId != senderId).Select(p => p.UserId).ToList();

        public ChatMessage SendMessage(Guid senderId, string content, IEnumerable<Attachment>? attachments, AuditInfo auditInfo)
        {
            if (!HasActiveParticipant(senderId))
                throw new ChatDomainException("Sender is not a participant of this chat room.");

            var message = ChatMessage.Create(Id, senderId, content, attachments, GetRecipientIds(senderId), auditInfo);
            LastMessageOn = message.SentOn;
            return message;
        }
    }
}
