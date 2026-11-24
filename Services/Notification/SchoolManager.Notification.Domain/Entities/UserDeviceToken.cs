using BuildingBlocks;

namespace SchoolManager.Notification.Domain
{
    /// <summary>
    /// A Firebase Cloud Messaging token of one app on one device, for push notifications.
    /// A token belongs to one user at a time: when another user signs in on the same device it is reassigned.
    /// </summary>
    public sealed class UserDeviceToken : BaseEntity
    {
        public const int TokenMaxLength = 512;
        public const int AppNameMaxLength = 20;

        /// <summary>App names sent by the mobile apps</summary>
        public static readonly IReadOnlyList<string> AppNames = new[] { "parent", "teacher", "driver" };

        /// <summary>Identity user id (parent id / teacher id / bus id for drivers)</summary>
        public Guid UserId { get; private set; }
        public DeviceUserType UserType { get; private set; }
        public string Token { get; private set; } = default!;
        public DevicePlatform Platform { get; private set; }

        /// <summary>"parent" | "teacher" | "driver"</summary>
        public string AppName { get; private set; } = default!;

        /// <summary>Last time the app registered the token (UTC)</summary>
        public DateTime LastSeenAt { get; private set; }
        public AuditInfo AuditInfo { get; private set; } = default!;

        private UserDeviceToken() { }

        public UserDeviceToken(Guid userId, DeviceUserType userType, string token, DevicePlatform platform, string appName, AuditInfo auditInfo)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User is required.", nameof(userId));
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token is required.", nameof(token));

            UserId = userId;
            UserType = userType;
            Token = token.Trim();
            Platform = platform;
            AppName = appName.Trim().ToLowerInvariant();
            AuditInfo = auditInfo;
            LastSeenAt = DateTime.UtcNow;
        }

        /// <summary>Another user signed in on this device: the token now belongs to them</summary>
        public void Reassign(Guid userId, DeviceUserType userType, AuditInfo auditInfo)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User is required.", nameof(userId));

            UserId = userId;
            UserType = userType;
            AuditInfo = auditInfo;
            Touch();
        }

        /// <summary>The app registered the token again (app start / login)</summary>
        public void Touch(DevicePlatform? platform = null, string? appName = null)
        {
            if (platform is DevicePlatform p) Platform = p;
            if (!string.IsNullOrWhiteSpace(appName)) AppName = appName.Trim().ToLowerInvariant();
            LastSeenAt = DateTime.UtcNow;
        }
    }
}
