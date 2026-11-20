using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class PeriodAttendanceDto
    {
        public Guid Id { get; set; }
        public Guid DailyAttendanceId { get; set; }

        /// <summary>The student of the day (filled from the DailyAttendance)</summary>
        public Guid StudentId { get; set; }
        public Guid TimetableSlotId { get; set; }
        public Guid SubjectId { get; set; }
        public Guid TeacherId { get; set; }
        public TimeOnly StartTime { get; set; }
        public PeriodAttendanceStatus Status { get; set; }
        public string? AbsenceReason { get; set; }

        /// <summary>UTC</summary>
        public DateTime RecordedAt { get; set; }
    }
}
