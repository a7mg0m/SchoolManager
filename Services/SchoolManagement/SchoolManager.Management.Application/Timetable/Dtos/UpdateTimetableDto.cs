namespace SchoolManager.Management.Application
{
    public class UpdateTimetableDto
    {
        public Guid Id { get; set; }
        public Guid GradeId { get; set; }
        public Guid AcademicYearId { get; set; }
    }
}
