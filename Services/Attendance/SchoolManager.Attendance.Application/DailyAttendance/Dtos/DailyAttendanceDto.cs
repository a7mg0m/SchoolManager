using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class DailyAttendanceDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid SectionId { get; set; }
        public Guid GradeId { get; set; }
        public Guid AcademicYearId { get; set; }
        public DateOnly Date { get; set; }
        public DailyAttendanceStatus Status { get; set; }
        public AttendanceSource Source { get; set; }
        public TimeOnly? CheckInTime { get; set; }
        public TimeOnly? CheckOutTime { get; set; }
        public string? Notes { get; set; }

        /// <summary>Lessons attended (present or late)</summary>
        public int AttendedPeriodsCount { get; set; }
        public int AbsentPeriodsCount { get; set; }
        public List<PeriodAttendanceDto> Periods { get; set; } = new();
    }
}
