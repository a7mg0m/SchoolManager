using BuildingBlocks;

namespace SchoolManager.Notification.Domain
{
    /// <summary>
    /// Whether an invited student / teacher came to a school event, marked by the admin on the day (check-in).
    /// One row per event and person; people not marked yet have no row.
    /// </summary>
    public sealed class EventAttendance : BaseEntity
    {
        public Guid EventId { get; private set; }
        public EventAttendeeType AttendeeType { get; private set; }

        /// <summary>The student id or the teacher id (Management)</summary>
        public Guid AttendeeId { get; private set; }

        public bool IsPresent { get; private set; }

        public AuditInfo AuditInfo { get; private set; } = null!;

        private EventAttendance() { }

        public EventAttendance(Guid eventId, EventAttendeeType attendeeType, Guid attendeeId, bool isPresent, AuditInfo auditInfo)
        {
            if (eventId == Guid.Empty) throw new ArgumentException("EventRequired", nameof(eventId));
            if (attendeeId == Guid.Empty) throw new ArgumentException("AttendeeRequired", nameof(attendeeId));

            EventId = eventId;
            AttendeeType = attendeeType;
            AttendeeId = attendeeId;
            IsPresent = isPresent;
            AuditInfo = auditInfo;
        }

        public void Mark(bool isPresent, Guid userId)
        {
            if (IsPresent == isPresent) return;
            IsPresent = isPresent;
            AuditInfo = new AuditInfo(AuditInfo.CreatedOn, AuditInfo.CreatedBy, DateTime.Now, userId);
        }
    }
}
