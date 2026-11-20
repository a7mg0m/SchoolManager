namespace BuildingBlocks.Messaging.Events
{
    /// <summary>
    /// A teacher was created, updated or deleted in the Management service.
    /// Published on <see cref="Topics.ManagementTeachers"/> with the teacher id as key,
    /// so all events of one teacher keep their order. The Identity service keeps the
    /// teacher's login User (same id as the teacher) and UserProfile in sync.
    /// For <see cref="TeacherChangeType.Deleted"/> only TeacherId is meaningful.
    /// </summary>
    public sealed record TeacherIntegrationEvent : IntegrationEvent
    {
        public TeacherChangeType ChangeType { get; init; }
        public Guid TeacherId { get; init; }
        public string FullNameAr { get; init; } = string.Empty;
        public string FullNameEn { get; init; } = string.Empty;
        public string NationalId { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string? Email { get; init; }
        public string? Address { get; init; }
        public bool IsActive { get; init; }

        /// <summary>Login name of the teacher's account; when empty the National ID is used.</summary>
        public string? LoginName { get; init; }

        /// <summary>
        /// BCrypt hash of a new password entered in Management, or null to keep the current password
        /// (a new account without one gets the National ID as first password). Never the plain password.
        /// </summary>
        public string? PasswordHash { get; init; }
        /// <summary>User who made the change in Management (for the audit columns).</summary>
        public Guid ChangedBy { get; init; }
    }
}
