namespace SchoolManager.Management.Application
{
    public class UpdateStudentDto
    {
        public Guid Id { get; set; }
        public string FullNameAr { get; set; } = string.Empty;
        public string FullNameEn { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Address { get; set; } = string.Empty;
        public Guid GradeId { get; set; }
        public Guid SectionId { get; set; }
        public Guid AcademicYearId { get; set; }
    }
}
