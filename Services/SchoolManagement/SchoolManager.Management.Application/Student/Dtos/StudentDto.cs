using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class StudentDto
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
        /// <summary>Home location (map pin); both or neither</summary>
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Guid StageId { get; set; }
        public Guid GradeId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid AcademicYearId { get; set; }
        public Guid ParentId { get; set; }
        public bool IsActive { get; set; }
        /// <summary>1 Current, 2 Graduated, 3 Transferred</summary>
        public StudentStatus Status { get; set; }
        public DateOnly? StatusDate { get; set; }
        public string? StatusNote { get; set; }
    }
}
