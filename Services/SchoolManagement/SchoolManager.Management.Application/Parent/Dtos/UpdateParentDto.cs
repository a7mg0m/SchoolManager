namespace SchoolManager.Management.Application
{
    public class UpdateParentDto
    {
        public Guid Id { get; set; }
        public string FullNameAr { get; set; } = string.Empty;
        public string FullNameEn { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Address { get; set; } = string.Empty;
        public List<ParentStudentDto> Students { get; set; } = new();

        /// <summary>Login name of the parent's account (required).</summary>
        public string? LoginName { get; set; }

        /// <summary>New password, or null / empty to keep the current one.</summary>
        public string? Password { get; set; }
    }
}
