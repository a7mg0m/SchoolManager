namespace SchoolManager.Identity.Application
{
    /// <summary>Profile of the logged-in user (User + UserProfile + role).</summary>
    public class MyProfileDto
    {
        /// <summary>UserProfile id, or null when the user has no profile yet.</summary>
        public Guid? Id { get; set; }
        public Guid UserId { get; set; }
        public string LoginName { get; set; } = string.Empty;
        public string? RoleNameAr { get; set; }
        public string? RoleNameEn { get; set; }
        public DateTime? LastLogin { get; set; }
        /// <summary>True when the user belongs to an employee (email / phone / address are synced to it).</summary>
        public bool IsEmployee { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public byte[]? ProfileImage { get; set; }
    }
}
