namespace SchoolManager.Management.Application
{
    public class AcademicYearSemesterDto
    {
        public Guid? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }
}
