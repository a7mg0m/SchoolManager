using BuildingBlocks;
using BuildingBlocks.Messaging.Events;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// Checks and prepares a bus driver's login name and password before the bus is saved
    /// (same limits as teacher and parent logins). Errors are <see cref="DomainException"/> with an error key.
    /// The Identity service creates the login from <see cref="BusDriverIntegrationEvent"/> and checks the name
    /// against every other user too.
    /// </summary>
    public static class BusDriverAccount
    {
        public const int LoginNameMinLength = 3;
        public const int LoginNameMaxLength = 100;
        public const int PasswordMinLength = 6;

        public const string LoginNameRequired = "LoginNameRequired";
        public const string LoginNameTooShort = "LoginNameTooShort";
        public const string LoginNameTooLong = "LoginNameTooLong";
        public const string LoginNameExists = "LoginNameExists";
        public const string PasswordRequired = "PasswordRequired";
        public const string PasswordTooShort = "PasswordTooShort";

        /// <summary>Trimmed login name; throws when it is missing, too short / long, or another bus's driver has it.</summary>
        public static async Task<string> ValidateLoginNameAsync(string? loginName, Guid busId, IBusRepository buses)
        {
            var value = loginName?.Trim() ?? string.Empty;
            if (value.Length == 0)
                throw new DomainException(LoginNameRequired);
            if (value.Length < LoginNameMinLength)
                throw new DomainException(LoginNameTooShort);
            if (value.Length > LoginNameMaxLength)
                throw new DomainException(LoginNameTooLong);

            var same = await buses.SearchAsync(b => b.DriverLoginName == value);
            if (same.Any(b => b.Id != busId))
                throw new DomainException(LoginNameExists);

            return value;
        }

        /// <summary>
        /// BCrypt hash of the password (same as the Identity service), or null when none was entered and
        /// <paramref name="required"/> is false (keep the current password). Only the hash leaves this service.
        /// </summary>
        public static string? HashPassword(string? password, bool required)
        {
            if (string.IsNullOrEmpty(password))
                return required ? throw new DomainException(PasswordRequired) : null;
            if (password.Length < PasswordMinLength)
                throw new DomainException(PasswordTooShort);

            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <param name="passwordHash">BCrypt hash of a new password, or null to keep the current one.</param>
        public static BusDriverIntegrationEvent Event(Bus bus, BusDriverChangeType changeType, Guid changedBy, string? passwordHash = null)
            => new()
            {
                ChangeType = changeType,
                BusId = bus.Id,
                DriverName = bus.DriverName,
                DriverPhone = bus.DriverPhone,
                IsActive = bus.IsActive,
                LoginName = bus.DriverLoginName,
                PasswordHash = passwordHash,
                ChangedBy = changedBy
            };

        public static BusDriverIntegrationEvent Deleted(Guid busId, Guid changedBy)
            => new() { ChangeType = BusDriverChangeType.Deleted, BusId = busId, ChangedBy = changedBy };
    }
}
