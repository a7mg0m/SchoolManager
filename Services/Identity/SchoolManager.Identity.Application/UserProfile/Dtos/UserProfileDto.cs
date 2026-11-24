namespace SchoolManager.Identity.Application
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public byte[]? ProfileImage { get; set; }

        /// <summary>Linked employee, teacher and parent (null when the user is not one).</summary>
        public Guid? EmployeeId { get; set; }
        public Guid? TeacherId { get; set; }
        public Guid? ParentId { get; set; }
        public Guid? BusId { get; set; }
    }
}
