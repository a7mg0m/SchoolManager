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
        /// <summary>The teacher no longer works at the school</summary>
        public bool IsServiceEnded { get; set; }
        /// <summary>Last day of service (yyyy-MM-dd); today when ended without a date</summary>
        public DateOnly? ServiceEndDate { get; set; }

        /// <summary>Job title (المسمى الوظيفي), e.g. "Math teacher"</summary>
        public string? JobTitle { get; set; }
        /// <summary>Staff number at the school</summary>
        public string? EmployeeNumber { get; set; }
        /// <summary>Highest qualification (المؤهل), e.g. "Bachelor of Education"</summary>
        public string? Qualification { get; set; }
        /// <summary>Specialization / major (التخصص)</summary>
        public string? Specialization { get; set; }
        /// <summary>Date the teacher joined the school</summary>
        public DateOnly? HireDate { get; set; }
        /// <summary>Total years of teaching experience</summary>
        public int? YearsOfExperience { get; set; }

        /// <summary>Login name of the teacher's account (required).</summary>
        public string? LoginName { get; set; }

        /// <summary>Password of the account: required on create; on update empty keeps the current one.</summary>
        public string? Password { get; set; }

    }
}
