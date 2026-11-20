using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class TripDto
    {
        public Guid Id { get; set; }
        public Guid BusId { get; set; }
        public DateOnly Date { get; set; }
        public TripType Type { get; set; }
        public TripStatus Status { get; set; }

        /// <summary>UTC</summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>UTC</summary>
        public DateTime? EndedAt { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }

        /// <summary>UTC</summary>
        public DateTime? LastLocationAt { get; set; }
        public List<TripStudentDto> Students { get; set; } = new();
    }
}
