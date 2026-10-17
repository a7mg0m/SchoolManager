namespace SchoolManager.Management.Application
{
    public class CreateAcademicYearDto
    {
        public string Name { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }
}
