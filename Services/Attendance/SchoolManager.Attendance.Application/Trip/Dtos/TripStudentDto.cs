using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class TripStudentDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public TripStudentStatus Status { get; set; }

        /// <summary>UTC</summary>
        public DateTime? BoardedAt { get; set; }

        /// <summary>UTC</summary>
        public DateTime? DroppedOffAt { get; set; }
    }
}
