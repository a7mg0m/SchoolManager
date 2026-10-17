namespace SchoolManager.Management.Application
{
    public class UpdateSemesterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid AcademicYearId { get; set; }
    }
}
