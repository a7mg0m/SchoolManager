using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>Parent app: one trip of the child (trip list), with the bus and the child's status on it</summary>
    public class ChildTripDto
    {
        public Guid TripId { get; set; }
        public DateOnly Date { get; set; }
        public TripType Type { get; set; }
        public TripStatus Status { get; set; }

        /// <summary>UTC</summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>UTC</summary>
        public DateTime? EndedAt { get; set; }

        public Guid BusId { get; set; }
        public string BusNumber { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;

        // The child on this trip
        public TripStudentStatus StudentStatus { get; set; }

        /// <summary>UTC</summary>
        public DateTime? BoardedAt { get; set; }

        /// <summary>UTC</summary>
        public DateTime? DroppedOffAt { get; set; }
    }
}
