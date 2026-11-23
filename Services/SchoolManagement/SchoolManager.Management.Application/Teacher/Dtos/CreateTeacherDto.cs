namespace SchoolManager.Management.Application
{
    public class CreateTeacherDto
    {
        public string FullNameAr { get; set; } = string.Empty;
        public string FullNameEn { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        /// <summary>Job level (free text, optional)</summary>
        public string? JobLevel { get; set; }

        /// <summary>Login name of the teacher's account (required).</summary>
        public string? LoginName { get; set; }

        /// <summary>Password of the account: required on create; on update empty keeps the current one.</summary>
        public string? Password { get; set; }

    }
}
