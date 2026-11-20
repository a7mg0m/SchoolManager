using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>Parent app: today's running trip of the child, to follow the bus</summary>
    public class TripTrackingDto
    {
        public Guid TripId { get; set; }
        public TripType Type { get; set; }

        /// <summary>UTC</summary>
        public DateTime? StartedAt { get; set; }

        public Guid BusId { get; set; }
        public string BusNumber { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;

        // Last known position of the bus
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }

        /// <summary>UTC</summary>
        public DateTime? LastLocationAt { get; set; }

        // The child on this trip
        public TripStudentStatus StudentStatus { get; set; }

        /// <summary>UTC</summary>
        public DateTime? BoardedAt { get; set; }

        /// <summary>UTC</summary>
        public DateTime? DroppedOffAt { get; set; }
    }
}
