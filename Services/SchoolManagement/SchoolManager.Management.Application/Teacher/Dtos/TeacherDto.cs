namespace SchoolManager.Management.Application
{
    public class TeacherDto
    {
        public Guid Id { get; set; }
        public string FullNameAr { get; set; } = string.Empty;
        public string FullNameEn { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        /// <summary>Job level (free text); null when not set</summary>
        public string? JobLevel { get; set; }
        /// <summary>The teacher no longer works at the school</summary>
        public bool IsServiceEnded { get; set; }
        /// <summary>Last day of service (yyyy-MM-dd); today when ended without a date</summary>
        public DateOnly? ServiceEndDate { get; set; }

        /// <summary>Login name of the teacher's account (null for teachers saved before it existed)</summary>
        public string? LoginName { get; set; }
    }
}
