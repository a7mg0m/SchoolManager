namespace BuildingBlocks.Messaging.Events
{
    /// <summary>
    /// A parent was created, updated or deleted in the Management service.
    /// Published on <see cref="Topics.ManagementParents"/> with the parent id as key,
    /// so all events of one parent keep their order. The Identity service keeps the
    /// parent's login User (same id as the parent) and UserProfile (ParentId) in sync.
    /// For <see cref="ParentChangeType.Deleted"/> only ParentId is meaningful.
    /// </summary>
    public sealed record ParentIntegrationEvent : IntegrationEvent
    {
        public ParentChangeType ChangeType { get; init; }
        public Guid ParentId { get; init; }
        public string FullNameAr { get; init; } = string.Empty;
        public string FullNameEn { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;

        /// <summary>Login name of the parent's account; when empty the phone number is used.</summary>
        public string? LoginName { get; init; }

        /// <summary>
        /// BCrypt hash of a new password entered in Management, or null to keep the current password
        /// (a new account without one gets the phone number as first password). Never the plain password.
        /// </summary>
        public string? PasswordHash { get; init; }
        public string? Email { get; init; }
        public string? Address { get; init; }
        /// <summary>User who made the change in Management (for the audit columns).</summary>
        public Guid ChangedBy { get; init; }
    }
}
