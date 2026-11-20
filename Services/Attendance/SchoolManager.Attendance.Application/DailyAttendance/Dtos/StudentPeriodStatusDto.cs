using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>One student's attendance in the lesson being recorded</summary>
    public class StudentPeriodStatusDto
    {
        public Guid StudentId { get; set; }
        public PeriodAttendanceStatus Status { get; set; }

        /// <summary>Kept only for an absent / excused student</summary>
        public string? AbsenceReason { get; set; }
    }
}
