namespace BuildingBlocks.Messaging.Events
{
    /// <summary>
    /// A student's bus fee was assigned, changed or ended in the Attendance service (bus assignment).
    /// Published on <see cref="Topics.AttendanceBusFees"/> with the bus assignment id as key,
    /// so all events of one assignment keep their order. The Financial service creates / updates a
    /// Transportation StudentFee from it. The fee is per student (one bus carries students of different
    /// grades), not taken from the grade fees of the academic year.
    /// </summary>
    public sealed record BusFeeIntegrationEvent : IntegrationEvent
    {
        public BusFeeChangeType ChangeType { get; init; }
        public Guid BusAssignmentId { get; init; }
        public Guid StudentId { get; init; }
        public Guid AcademicYearId { get; init; }
        /// <summary>The student's bus fee</summary>
        public decimal Amount { get; init; }
        public string Currency { get; init; } = string.Empty;
        /// <summary>Due date of the fee; set for <see cref="BusFeeChangeType.Assigned"/>, null to keep the current one.</summary>
        public DateTime? DueDate { get; init; }
        /// <summary>User who made the change in Attendance (for the audit columns).</summary>
        public Guid ChangedBy { get; init; }
    }
}
