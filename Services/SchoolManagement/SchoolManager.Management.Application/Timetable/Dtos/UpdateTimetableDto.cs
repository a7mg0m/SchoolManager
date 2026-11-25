namespace SchoolManager.Management.Application
{
    public class UpdateTimetableDto
    {
        public Guid Id { get; set; }
        public Guid StageId { get; set; }
        public Guid GradeId { get; set; }
        public Guid AcademicYearId { get; set; }
        public Guid SemesterId { get; set; }
        public List<TimetableSlotItemDto> Slots { get; set; } = new();

        /// <summary>When the first period of a work day starts</summary>
        public TimeOnly? DayStartTime { get; set; }

        /// <summary>The week settings; null keeps the current ones (update)</summary>
        public List<TimetableWorkDayDto>? WorkDays { get; set; }
    }
}
