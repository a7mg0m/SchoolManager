using BuildingBlocks.Messaging.Events;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>Builds the Kafka event sent to the Identity service when a parent changes.</summary>
    public static class ParentEventFactory
    {
        /// <param name="passwordHash">BCrypt hash of a new password, or null to keep the current one.</param>
        public static ParentIntegrationEvent From(Parent parent, ParentChangeType changeType, Guid changedBy, string? passwordHash = null)
            => new()
            {
                ChangeType = changeType,
                ParentId = parent.Id,
                FullNameAr = parent.FullName.Arabic,
                FullNameEn = parent.FullName.English,
                PhoneNumber = parent.PhoneNumber,
                LoginName = parent.LoginName,
                PasswordHash = passwordHash,
                Email = parent.Email,
                Address = parent.Address,
                ChangedBy = changedBy
            };

        public static ParentIntegrationEvent Deleted(Guid parentId, Guid changedBy)
            => new()
            {
                ChangeType = ParentChangeType.Deleted,
                ParentId = parentId,
                ChangedBy = changedBy
            };
    }
}
