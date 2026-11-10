namespace SchoolManager.Management.Application
{
    public class CreateSemesterDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid AcademicYearId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }
}
