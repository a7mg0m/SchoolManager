namespace SchoolManager.Management.Application
{
    public class CreateSemesterDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid AcademicYearId { get; set; }
    }
}
