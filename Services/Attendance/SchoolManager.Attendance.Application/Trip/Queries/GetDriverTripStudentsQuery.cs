using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>What the driver needs about a student on a trip (from the Management service)</summary>
    public record BusRiderInfo(
        Guid Id,
        string FullNameAr,
        string FullNameEn,
        string Gender,
        string GradeNameAr,
        string GradeNameEn,
        string SectionNameAr,
        string SectionNameEn,
        string ParentNameAr,
        string ParentNameEn,
        string ParentPhone,
        string Address,
        double? Latitude,
        double? Longitude);

    /// <summary>Students' names, classes and parents' phones from the Management service</summary>
    public interface IBusRiderDirectory
    {
        /// <summary>
        /// Asked with the driver's own token (the "Authorization" header value of the current request).
        /// Null when the Management service could not answer.
        /// </summary>
        Task<IReadOnlyList<BusRiderInfo>?> GetRidersAsync(IReadOnlyCollection<Guid> studentIds, string authorization, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Driver app: the students of a trip of the driver's bus, with their status, stop, class and parent's phone,
    /// in stop order (reversed on the way home). Null when the trip is not on that bus.
    /// </summary>
    public record GetDriverTripStudentsQuery(Guid TripId, Guid BusId, string Authorization) : IQuery<List<DriverTripStudentDto>?>;

    public class DriverTripStudentDto
    {
        public Guid StudentId { get; set; }
        public TripStudentStatus Status { get; set; }

        /// <summary>UTC</summary>
        public DateTime? BoardedAt { get; set; }

        /// <summary>UTC</summary>
        public DateTime? DroppedOffAt { get; set; }

        public string FullNameAr { get; set; } = string.Empty;
        public string FullNameEn { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string GradeNameAr { get; set; } = string.Empty;
        public string GradeNameEn { get; set; } = string.Empty;
        public string SectionNameAr { get; set; } = string.Empty;
        public string SectionNameEn { get; set; } = string.Empty;
        public string ParentNameAr { get; set; } = string.Empty;
        public string ParentNameEn { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        /// <summary>The student's stop on the bus's route (null when none was chosen)</summary>
        public string? StopNameAr { get; set; }
        public string? StopNameEn { get; set; }
        public int? StopOrder { get; set; }
    }

    public class GetDriverTripStudentsQueryHandler : IQueryHandler<GetDriverTripStudentsQuery, List<DriverTripStudentDto>?>
    {
        private readonly ITripRepository _trips;
        private readonly IBusAssignmentRepository _assignments;
        private readonly IBusRepository _buses;
        private readonly IBusRouteRepository _routes;
        private readonly IBusRiderDirectory _directory;

        public GetDriverTripStudentsQueryHandler(
            ITripRepository trips,
            IBusAssignmentRepository assignments,
            IBusRepository buses,
            IBusRouteRepository routes,
            IBusRiderDirectory directory)
        {
            _trips = trips;
            _assignments = assignments;
            _buses = buses;
            _routes = routes;
            _directory = directory;
        }

        public async Task<List<DriverTripStudentDto>?> Handle(GetDriverTripStudentsQuery query, CancellationToken cancellationToken)
        {
            var trip = await _trips.GetByIdAsync(query.TripId);
            if (trip is null || trip.BusId != query.BusId)
                return null;

            var studentIds = trip.Students.Select(s => s.StudentId).ToList();
            if (studentIds.Count == 0)
                return new List<DriverTripStudentDto>();

            // Each student's stop: from their assignment on this bus running on the trip's date
            var busId = trip.BusId;
            var date = trip.Date;
            var assignments = await _assignments.SearchAsync(a =>
                a.BusId == busId && studentIds.Contains(a.StudentId) && a.StartDate <= date && (a.EndDate == null || a.EndDate >= date));
            var stopByStudent = assignments
                .GroupBy(a => a.StudentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.StartDate).First().BusRouteStopId);

            var bus = await _buses.GetByIdAsync(busId);
            var route = bus is null ? null : await _routes.GetByIdAsync(bus.BusRouteId);
            var stops = route?.Stops.ToDictionary(s => s.Id) ?? new Dictionary<Guid, BusRouteStop>();

            // Names, classes and parents' phones (the trip still shows when Management does not answer)
            var riders = await _directory.GetRidersAsync(studentIds, query.Authorization, cancellationToken);
            var riderById = (riders ?? Array.Empty<BusRiderInfo>()).ToDictionary(r => r.Id);

            var result = trip.Students.Select(s =>
            {
                riderById.TryGetValue(s.StudentId, out var r);
                BusRouteStop? stop = null;
                if (stopByStudent.TryGetValue(s.StudentId, out var stopId) && stopId is Guid id)
                    stops.TryGetValue(id, out stop);

                return new DriverTripStudentDto
                {
                    StudentId = s.StudentId,
                    Status = s.Status,
                    BoardedAt = s.BoardedAt,
                    DroppedOffAt = s.DroppedOffAt,
                    FullNameAr = r?.FullNameAr ?? string.Empty,
                    FullNameEn = r?.FullNameEn ?? string.Empty,
                    Gender = r?.Gender ?? string.Empty,
                    GradeNameAr = r?.GradeNameAr ?? string.Empty,
                    GradeNameEn = r?.GradeNameEn ?? string.Empty,
                    SectionNameAr = r?.SectionNameAr ?? string.Empty,
                    SectionNameEn = r?.SectionNameEn ?? string.Empty,
                    ParentNameAr = r?.ParentNameAr ?? string.Empty,
                    ParentNameEn = r?.ParentNameEn ?? string.Empty,
                    ParentPhone = r?.ParentPhone ?? string.Empty,
                    Address = r?.Address ?? string.Empty,
                    Latitude = r?.Latitude,
                    Longitude = r?.Longitude,
                    StopNameAr = stop?.Name.Arabic,
                    StopNameEn = stop?.Name.English,
                    StopOrder = stop?.Order
                };
            });

            // Stop order (students without a stop last); on the way home the bus drives the route backwards
            var ordered = trip.Type == TripType.ToHome
                ? result.OrderBy(s => s.StopOrder is null).ThenByDescending(s => s.StopOrder)
                : result.OrderBy(s => s.StopOrder is null).ThenBy(s => s.StopOrder);
            return ordered.ThenBy(s => s.FullNameAr).ToList();
        }
    }
}
