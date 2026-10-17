namespace SchoolManager.Management.Application
{
    public class CreateTimetableSlotDto
    {
        public DayOfWeek Day { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public Guid SubjectId { get; set; }
        public Guid TeacherId { get; set; }
        public Guid GradeId { get; set; }
        public Guid SectionId { get; set; }
    }
}
