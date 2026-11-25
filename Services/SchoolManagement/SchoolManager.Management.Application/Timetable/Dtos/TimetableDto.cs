namespace SchoolManager.Management.Application
{
    public class TimetableDto
    {
        public Guid Id { get; set; }
        public Guid StageId { get; set; }
        public Guid GradeId { get; set; }
        public Guid AcademicYearId { get; set; }

        /// <summary>null: a timetable saved before semesters (used in every semester of its year)</summary>
        public Guid? SemesterId { get; set; }
        public List<TimetableSlotDto> Slots { get; set; } = new();

        /// <summary>When the first period of a work day starts; null when not set</summary>
        public TimeOnly? DayStartTime { get; set; }

        /// <summary>The week settings (empty for timetables saved before them)</summary>
        public List<TimetableWorkDayDto> WorkDays { get; set; } = new();
    }
}
