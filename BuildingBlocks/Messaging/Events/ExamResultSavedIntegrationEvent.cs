namespace BuildingBlocks.Messaging.Events
{
    /// <summary>
    /// A student's exam score was entered or changed (ExamsHomework service, admin results sheet or teacher app).
    /// Published on <see cref="Topics.ExamsExamResultSaved"/> with the exam id as key, one event per student. The
    /// Notification service turns it into a notification for that student's parents (shown in the parent app, pushed
    /// to their phones; not listed in the admin notifications page).
    /// </summary>
    public sealed record ExamResultSavedIntegrationEvent : IntegrationEvent
    {
        public Guid ExamId { get; init; }

        /// <summary>1 Quiz, 2 Monthly, 3 Mid-term, 4 Final, 5 Second final</summary>
        public int ExamType { get; init; }

        public Guid SubjectId { get; init; }
        public Guid GradeId { get; init; }
        public Guid? SectionId { get; init; }
        public Guid AcademicYearId { get; init; }
        public DateOnly ExamDate { get; init; }

        public Guid StudentId { get; init; }
        public double Score { get; init; }
        public double Weight { get; init; }
        public double SuccessScore { get; init; }

        /// <summary>The score replaced an earlier one (a correction)</summary>
        public bool IsCorrection { get; init; }

        /// <summary>User who saved it (for the audit columns)</summary>
        public Guid SavedBy { get; init; }
    }
}
