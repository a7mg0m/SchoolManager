namespace BuildingBlocks.Messaging.Events
{
    /// <summary>
    /// A bus (and so its driver) was created, updated or deleted in the Attendance service.
    /// Published on <see cref="Topics.AttendanceBusDrivers"/> with the bus id as key, so all events of one bus
    /// keep their order. The Identity service keeps the driver's login User (same id as the bus, role Driver)
    /// and UserProfile (BusId) in sync. For <see cref="BusDriverChangeType.Deleted"/> only BusId is meaningful.
    /// </summary>
    public sealed record BusDriverIntegrationEvent : IntegrationEvent
    {
        public BusDriverChangeType ChangeType { get; init; }
        public Guid BusId { get; init; }
        public string DriverName { get; init; } = string.Empty;
        public string DriverPhone { get; init; } = string.Empty;

        /// <summary>The bus is active: its driver can log in</summary>
        public bool IsActive { get; init; }

        /// <summary>Login name of the driver's account</summary>
        public string? LoginName { get; init; }

        /// <summary>BCrypt hash of a new password entered for the bus, or null to keep the current one. Never the plain password.</summary>
        public string? PasswordHash { get; init; }

        /// <summary>User who made the change in Attendance (for the audit columns).</summary>
        public Guid ChangedBy { get; init; }
    }
}
