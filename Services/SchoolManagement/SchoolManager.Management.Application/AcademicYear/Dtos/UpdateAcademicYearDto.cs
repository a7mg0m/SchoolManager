namespace SchoolManager.Management.Application
{
    public class UpdateAcademicYearDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public bool IsActive { get; set; }
        public List<AcademicYearSemesterDto>? Semesters { get; set; } = new();
    }
}
