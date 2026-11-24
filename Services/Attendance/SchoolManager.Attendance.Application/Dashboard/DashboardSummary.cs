using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// Admin home page: the day's attendance and the transport figures, counted in the database
    /// (one call instead of one per class / bus).
    /// </summary>
    /// <param name="Date">Day of the attendance and trip figures</param>
    /// <param name="AcademicYearId">Year of the attendance and bus riders; null = every year</param>
    public record GetDashboardSummaryQuery(DateOnly Date, Guid? AcademicYearId) : IQuery<DashboardSummaryDto>;

    public class DashboardSummaryDto
    {
        public DateOnly Date { get; set; }
        public DashboardAttendanceDto Attendance { get; set; } = new();
        public DashboardTransportDto Transport { get; set; } = new();
    }

    /// <summary>Students' days recorded on the date, by status</summary>
    public class DashboardAttendanceDto
    {
        public int Recorded { get; set; }
        public int Present { get; set; }
        public int PartiallyPresent { get; set; }
        public int Absent { get; set; }
        public int Excused { get; set; }

        /// <summary>Classes (sections) with at least one student recorded on the date</summary>
        public int SectionsRecorded { get; set; }
    }

    public class DashboardTransportDto
    {
        public int Buses { get; set; }
        public int ActiveBuses { get; set; }

        /// <summary>Seats of the active buses</summary>
        public int Seats { get; set; }

        /// <summary>Students with an active bus assignment (in the year)</summary>
        public int Riders { get; set; }

        /// <summary>Buses whose driver has no login yet</summary>
        public int DriversWithoutLogin { get; set; }

        public int TripsPlanned { get; set; }
        public int TripsInProgress { get; set; }
        public int TripsCompleted { get; set; }
        public int TripsCancelled { get; set; }

        /// <summary>Active buses, fullest first</summary>
        public List<DashboardBusDto> BusOccupancy { get; set; } = new();
    }

    public class DashboardBusDto
    {
        public Guid BusId { get; set; }
        public string Number { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int Riders { get; set; }
        public bool HasDriverLogin { get; set; }
    }

    public class GetDashboardSummaryQueryHandler : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
    {
        private readonly IDailyAttendanceRepository _attendance;
        private readonly IBusRepository _buses;
        private readonly IBusAssignmentRepository _assignments;
        private readonly ITripRepository _trips;

        public GetDashboardSummaryQueryHandler(
            IDailyAttendanceRepository attendance,
            IBusRepository buses,
            IBusAssignmentRepository assignments,
            ITripRepository trips)
        {
            _attendance = attendance;
            _buses = buses;
            _assignments = assignments;
            _trips = trips;
        }

        public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery query, CancellationToken cancellationToken)
        {
            var date = query.Date;
            var yearId = query.AcademicYearId;

            // Attendance of the day
            var days = await _attendance.SearchAsync(a => a.Date == date && (yearId == null || a.AcademicYearId == yearId));
            var attendance = new DashboardAttendanceDto
            {
                Recorded = days.Count,
                Present = days.Count(d => d.Status == DailyAttendanceStatus.Present),
                PartiallyPresent = days.Count(d => d.Status == DailyAttendanceStatus.PartiallyPresent),
                Absent = days.Count(d => d.Status == DailyAttendanceStatus.Absent),
                Excused = days.Count(d => d.Status == DailyAttendanceStatus.Excused),
                SectionsRecorded = days.Select(d => d.SectionId).Distinct().Count()
            };

            // Transport
            var buses = await _buses.GetAllAsync();
            var riders = await _assignments.SearchAsync(a => a.IsActive && (yearId == null || a.AcademicYearId == yearId));
            var ridersByBus = riders.GroupBy(a => a.BusId).ToDictionary(g => g.Key, g => g.Count());
            var trips = await _trips.SearchAsync(t => t.Date == date);
            var activeBuses = buses.Where(b => b.IsActive).ToList();

            var transport = new DashboardTransportDto
            {
                Buses = buses.Count,
                ActiveBuses = activeBuses.Count,
                Seats = activeBuses.Sum(b => b.Capacity),
                Riders = riders.Count,
                DriversWithoutLogin = buses.Count(b => b.DriverLoginName == null),
                TripsPlanned = trips.Count(t => t.Status == TripStatus.Planned),
                TripsInProgress = trips.Count(t => t.Status == TripStatus.InProgress),
                TripsCompleted = trips.Count(t => t.Status == TripStatus.Completed),
                TripsCancelled = trips.Count(t => t.Status == TripStatus.Cancelled),
                BusOccupancy = activeBuses
                    .Select(b => new DashboardBusDto
                    {
                        BusId = b.Id,
                        Number = b.Number,
                        DriverName = b.DriverName,
                        Capacity = b.Capacity,
                        Riders = ridersByBus.GetValueOrDefault(b.Id),
                        HasDriverLogin = b.DriverLoginName != null
                    })
                    .OrderByDescending(b => b.Capacity > 0 ? (double)b.Riders / b.Capacity : 0)
                    .ThenBy(b => b.Number)
                    .ToList()
            };

            return new DashboardSummaryDto { Date = date, Attendance = attendance, Transport = transport };
        }
    }
}
