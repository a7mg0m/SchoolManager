namespace BuildingBlocks.Messaging.Events
{
    /// <summary>
    /// A bus trip started (Attendance service, from the driver app or the admin page). Published on
    /// <see cref="Topics.AttendanceTripStarted"/> with the trip id as key. The Notification service tells the parents
    /// of the trip's students that the bus is on its way.
    /// </summary>
    public sealed record TripStartedIntegrationEvent : IntegrationEvent
    {
        public Guid TripId { get; init; }
        public Guid BusId { get; init; }
        public string BusNumber { get; init; } = string.Empty;
        public string PlateNumber { get; init; } = string.Empty;
        public string DriverName { get; init; } = string.Empty;

        /// <summary>0 = to school, 1 = to home</summary>
        public int TripType { get; init; }
        public DateOnly Date { get; init; }

        /// <summary>Academic year of the bus's assignments that day (the parent app lists notifications by year); null if unknown</summary>
        public Guid? AcademicYearId { get; init; }

        /// <summary>UTC</summary>
        public DateTime StartedAt { get; init; }

        /// <summary>The students on the trip</summary>
        public List<Guid> StudentIds { get; init; } = new();

        /// <summary>User who started it (the driver or an admin)</summary>
        public Guid StartedBy { get; init; }
    }
}
