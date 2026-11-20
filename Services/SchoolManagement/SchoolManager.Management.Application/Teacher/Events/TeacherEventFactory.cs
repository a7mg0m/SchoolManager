using BuildingBlocks.Messaging.Events;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>Builds the Kafka event sent to the Identity service when a teacher changes.</summary>
    public static class TeacherEventFactory
    {
        /// <param name="passwordHash">BCrypt hash of a new password, or null to keep the current one.</param>
        public static TeacherIntegrationEvent From(Teacher teacher, TeacherChangeType changeType, Guid changedBy, string? passwordHash = null)
            => new()
            {
                ChangeType = changeType,
                TeacherId = teacher.Id,
                FullNameAr = teacher.FullName.Arabic,
                FullNameEn = teacher.FullName.English,
                NationalId = teacher.NationalId,
                LoginName = teacher.LoginName,
                PasswordHash = passwordHash,
                PhoneNumber = teacher.PhoneNumber,
                Email = teacher.Email,
                Address = teacher.Address,
                IsActive = teacher.IsActive,
                ChangedBy = changedBy
            };

        public static TeacherIntegrationEvent Deleted(Guid teacherId, Guid changedBy)
            => new()
            {
                ChangeType = TeacherChangeType.Deleted,
                TeacherId = teacherId,
                ChangedBy = changedBy
            };
    }
}
