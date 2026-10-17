namespace SchoolManager.Management.Application
{
    public class SemesterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid AcademicYearId { get; set; }
        public bool IsActive { get; set; }
    }
}
