using AutoMapper;
using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class GetTripByIdQueryHandler : IQueryHandler<GetTripByIdQuery, TripDto?>
    {
        private readonly ITripRepository _repository;
        private readonly IMapper _mapper;

        public GetTripByIdQueryHandler(ITripRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<TripDto?> Handle(GetTripByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<TripDto>(entity);
        }
    }

    public class GetTripsByDateQueryHandler : IQueryHandler<GetTripsByDateQuery, List<TripDto>>
    {
        private readonly ITripRepository _repository;
        private readonly IMapper _mapper;

        public GetTripsByDateQueryHandler(ITripRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<TripDto>> Handle(GetTripsByDateQuery query, CancellationToken cancellationToken)
        {
            var busId = query.BusId;
            var entities = await _repository.SearchAsync(t => t.Date == query.Date && (busId == null || t.BusId == busId));
            return _mapper.Map<List<TripDto>>(entities.OrderBy(t => t.Type).ToList());
        }
    }

    /// <summary>
    /// Parent: the child's trips (the child must be the parent's) between two dates, newest first, with the bus and the
    /// child's status on each. Null when Management could not tell the parent's children.
    /// </summary>
    public class GetMyChildTripsQueryHandler : IQueryHandler<GetMyChildTripsQuery, List<ChildTripDto>?>
    {
        private readonly ITripRepository _repository;
        private readonly IBusRepository _busRepository;
        private readonly IParentChildrenService _parentChildren;
        private readonly ICurrentUserService _currentUser;

        public GetMyChildTripsQueryHandler(
            ICurrentUserService currentUser,
            ITripRepository repository,
            IBusRepository busRepository,
            IParentChildrenService parentChildren)
        {
            _currentUser = currentUser;
            _repository = repository;
            _busRepository = busRepository;
            _parentChildren = parentChildren;
        }

        public async Task<List<ChildTripDto>?> Handle(GetMyChildTripsQuery query, CancellationToken cancellationToken)
        {
            var children = await ParentChildrenCheck.MyChildrenAsync(_currentUser, _parentChildren, query.Authorization, cancellationToken);
            if (children is null) return null;
            ParentChildrenCheck.EnsureMyChild(children, query.StudentId);

            var studentId = query.StudentId;
            var from = query.From;
            var to = query.To;
            var trips = await _repository.SearchAsync(t => t.Date >= from && t.Date <= to && t.Students.Any(s => s.StudentId == studentId));

            // The buses of these trips (one lookup each)
            var buses = new Dictionary<Guid, Bus?>();
            foreach (var busId in trips.Select(t => t.BusId).Distinct())
                buses[busId] = await _busRepository.GetByIdAsync(busId);

            return trips
                .OrderByDescending(t => t.Date).ThenByDescending(t => t.Type)
                .Select(t =>
                {
                    var student = t.Students.First(s => s.StudentId == studentId);
                    var bus = buses.GetValueOrDefault(t.BusId);
                    return new ChildTripDto
                    {
                        TripId = t.Id,
                        Date = t.Date,
                        Type = t.Type,
                        Status = t.Status,
                        StartedAt = t.StartedAt,
                        EndedAt = t.EndedAt,
                        BusId = t.BusId,
                        BusNumber = bus?.Number ?? string.Empty,
                        PlateNumber = bus?.PlateNumber ?? string.Empty,
                        DriverName = bus?.DriverName ?? string.Empty,
                        StudentStatus = student.Status,
                        BoardedAt = student.BoardedAt,
                        DroppedOffAt = student.DroppedOffAt
                    };
                })
                .ToList();
        }
    }

    /// <summary>
    /// Parent: the child (must be the parent's) on a running trip today, with the bus, driver, last position and the
    /// child's status on it. Throws when Management could not tell the parent's children.
    /// </summary>
    public class GetMyChildCurrentTripQueryHandler : IQueryHandler<GetMyChildCurrentTripQuery, TripTrackingDto?>
    {
        private readonly ITripRepository _repository;
        private readonly IBusRepository _busRepository;
        private readonly IParentChildrenService _parentChildren;
        private readonly ICurrentUserService _currentUser;

        public GetMyChildCurrentTripQueryHandler(
            ICurrentUserService currentUser,
            ITripRepository repository,
            IBusRepository busRepository,
            IParentChildrenService parentChildren)
        {
            _currentUser = currentUser;
            _repository = repository;
            _busRepository = busRepository;
            _parentChildren = parentChildren;
        }

        public async Task<TripTrackingDto?> Handle(GetMyChildCurrentTripQuery query, CancellationToken cancellationToken)
        {
            var children = await ParentChildrenCheck.MyChildrenAsync(_currentUser, _parentChildren, query.Authorization, cancellationToken)
                ?? throw new DomainException("Your children could not be checked right now. Please try again later.");
            ParentChildrenCheck.EnsureMyChild(children, query.StudentId);

            var studentId = query.StudentId;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var trip = (await _repository.SearchAsync(t => t.Date == today && t.Status == TripStatus.InProgress
                    && t.Students.Any(s => s.StudentId == studentId)))
                .OrderByDescending(t => t.StartedAt)
                .FirstOrDefault();
            if (trip is null) return null;

            var student = trip.Students.First(s => s.StudentId == studentId);
            var bus = await _busRepository.GetByIdAsync(trip.BusId);

            return new TripTrackingDto
            {
                TripId = trip.Id,
                Type = trip.Type,
                StartedAt = trip.StartedAt,
                BusId = trip.BusId,
                BusNumber = bus?.Number ?? string.Empty,
                PlateNumber = bus?.PlateNumber ?? string.Empty,
                DriverName = bus?.DriverName ?? string.Empty,
                DriverPhone = bus?.DriverPhone ?? string.Empty,
                CurrentLatitude = trip.CurrentLatitude,
                CurrentLongitude = trip.CurrentLongitude,
                LastLocationAt = trip.LastLocationAt,
                StudentStatus = student.Status,
                BoardedAt = student.BoardedAt,
                DroppedOffAt = student.DroppedOffAt
            };
        }
    }
}
