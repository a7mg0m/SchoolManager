namespace SchoolManager.Management.Application
{
    public class TimetableDto
    {
        public Guid Id { get; set; }
        public Guid StageId { get; set; }
        public Guid GradeId { get; set; }
        public Guid AcademicYearId { get; set; }
        public List<TimetableSlotDto> Slots { get; set; } = new();
    }
}
