namespace BuildingBlocks.Messaging.Events
{
    /// <summary>
    /// A teacher published a homework to classes (ExamsHomework service). Published on
    /// <see cref="Topics.ExamsHomeworkPublished"/> with the homework id as key. The Notification service turns it into a
    /// notification for the parents of those classes (shown in the parent app, pushed to their phones; not listed in the
    /// admin notifications page).
    /// </summary>
    public sealed record HomeworkPublishedIntegrationEvent : IntegrationEvent
    {
        public Guid HomeworkId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public DateOnly DueDate { get; init; }
        public Guid SubjectId { get; init; }
        public Guid TeacherId { get; init; }
        public Guid GradeId { get; init; }
        public Guid AcademicYearId { get; init; }
        public Guid SemesterId { get; init; }

        /// <summary>The classes it was newly published to (classes published to before are not repeated)</summary>
        public List<Guid> SectionIds { get; init; } = new();

        /// <summary>User who published it (for the audit columns)</summary>
        public Guid PublishedBy { get; init; }
    }
}
