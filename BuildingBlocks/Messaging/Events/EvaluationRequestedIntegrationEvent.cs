namespace BuildingBlocks.Messaging.Events
{
    /// <summary>
    /// A parent asked a teacher to evaluate their child (Attendance service, parent app). Published on
    /// <see cref="Topics.AttendanceEvaluationRequested"/> with the request id as key. The Notification service turns it
    /// into a notification for that teacher (shown in the teacher app, pushed to their phone; not listed in the admin
    /// notifications page).
    /// </summary>
    public sealed record EvaluationRequestedIntegrationEvent : IntegrationEvent
    {
        public Guid RequestId { get; init; }
        public Guid TeacherId { get; init; }
        public Guid StudentId { get; init; }

        /// <summary>The child's name (empty when Management could not say)</summary>
        public string StudentNameAr { get; init; } = string.Empty;
        public string StudentNameEn { get; init; } = string.Empty;

        /// <summary>The subject asked about (null: in general)</summary>
        public Guid? SubjectId { get; init; }

        /// <summary>The parent's note</summary>
        public string? Note { get; init; }

        public DateTime RequestedAt { get; init; }

        /// <summary>The child's academic year (the teacher app lists notifications by year)</summary>
        public Guid? AcademicYearId { get; init; }

        /// <summary>User who asked (the parent; for the audit columns)</summary>
        public Guid RequestedBy { get; init; }
    }
}
