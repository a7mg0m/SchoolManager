namespace BuildingBlocks.Messaging.Events
{
    /// <summary>
    /// A new exam was saved (ExamsHomework service, from the admin page or the teacher app). Published on
    /// <see cref="Topics.ExamsExamCreated"/> with the exam id as key. The Notification service turns it into a
    /// notification for the parents of the exam's class (or of the whole grade when it has no class).
    /// </summary>
    public sealed record ExamCreatedIntegrationEvent : IntegrationEvent
    {
        public Guid ExamId { get; init; }

        /// <summary>1 Quiz, 2 Monthly, 3 MidTerm, 4 Final</summary>
        public int ExamType { get; init; }
        public Guid SubjectId { get; init; }
        public Guid GradeId { get; init; }

        /// <summary>null: the exam is for the whole grade</summary>
        public Guid? SectionId { get; init; }
        public Guid AcademicYearId { get; init; }
        public Guid SemesterId { get; init; }
        public DateOnly ExamDate { get; init; }
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }

        /// <summary>Full mark</summary>
        public double Weight { get; init; }

        /// <summary>Pass mark</summary>
        public double SuccessScore { get; init; }

        /// <summary>User who saved it (for the audit columns)</summary>
        public Guid CreatedBy { get; init; }
    }
}
