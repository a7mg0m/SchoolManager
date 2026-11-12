namespace SchoolManager.Management.Application
{
    public class CreateTimetableDto
    {
        public Guid StageId { get; set; }
        public Guid GradeId { get; set; }
        public Guid AcademicYearId { get; set; }
        public List<CreateTimetableSlotDto> Slots { get; set; } = new();
    }
}
