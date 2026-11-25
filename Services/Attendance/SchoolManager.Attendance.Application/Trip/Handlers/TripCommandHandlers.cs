using BuildingBlocks.Messaging.Events;
using BuildingBlocks.Messaging;
using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// Plans the trip: the bus must be active, with no other trip (not cancelled) for the same date and direction.
    /// Students: the bus's active assignments that run on that date (started, not ended before it).
    /// </summary>
    public class CreateTripCommandHandler : ICommandHandler<CreateTripCommand, Guid>
    {
        private readonly ITripRepository _repository;
        private readonly IBusRepository _busRepository;
        private readonly IBusAssignmentRepository _assignmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateTripCommandHandler(
            ICurrentUserService currentUser,
            ITripRepository repository,
            IBusRepository busRepository,
            IBusAssignmentRepository assignmentRepository,
            IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _busRepository = busRepository;
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateTripCommand command, CancellationToken cancellationToken)
        {
            var bus = await _busRepository.GetByIdAsync(command.BusId)
                ?? throw new DomainException("Bus not found.");
            if (!bus.IsActive)
                throw new DomainException("The bus is not active.");

            var busId = bus.Id;
            var date = command.Date;
            var type = command.Type;
            var existing = await _repository.SearchAsync(t => t.BusId == busId && t.Date == date && t.Type == type
                && t.Status != TripStatus.Cancelled);
            if (existing.Count > 0)
                throw new DomainException("The bus already has this trip on this date.");

            var trip = Trip.Create(busId, date, type, new AuditInfo(DateTime.Now, _currentUser.UserId));

            var riders = await _assignmentRepository.SearchAsync(a => a.BusId == busId && a.IsActive
                && a.StartDate <= date && (a.EndDate == null || a.EndDate >= date));
            foreach (var studentId in riders.Select(a => a.StudentId).Distinct())
                trip.AddStudent(studentId);

            await _repository.AddAsync(trip);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return trip.Id;
        }
    }

    /// <summary>
    /// The bus leaves; the parents of the trip's students are told (Notification service, through Kafka — the event is
    /// saved in the same transaction).
    /// </summary>
    public class StartTripCommandHandler : ICommandHandler<StartTripCommand, bool>
    {
        private readonly ITripRepository _repository;
        private readonly IBusRepository _buses;
        private readonly IBusAssignmentRepository _assignments;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIntegrationEventOutbox _outbox;
        private readonly ICurrentUserService _currentUser;

        public StartTripCommandHandler(
            ITripRepository repository,
            IBusRepository buses,
            IBusAssignmentRepository assignments,
            IUnitOfWork unitOfWork,
            IIntegrationEventOutbox outbox,
            ICurrentUserService currentUser)
        {
            _repository = repository;
            _buses = buses;
            _assignments = assignments;
            _unitOfWork = unitOfWork;
            _outbox = outbox;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(StartTripCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            var startedAt = DateTime.UtcNow;
            entity.Start(startedAt);
            await _repository.UpdateAsync(entity);

            var bus = await _buses.GetByIdAsync(entity.BusId);

            // The academic year of the bus's assignments that day: the notification belongs to it
            var busId = entity.BusId;
            var date = entity.Date;
            var yearId = (await _assignments.SearchAsync(a =>
                    a.BusId == busId && a.StartDate <= date && (a.EndDate == null || a.EndDate >= date)))
                .GroupBy(a => a.AcademicYearId)
                .OrderByDescending(g => g.Count())
                .Select(g => (Guid?)g.Key)
                .FirstOrDefault();

            _outbox.Enqueue(Topics.AttendanceTripStarted, entity.Id.ToString(), new TripStartedIntegrationEvent
            {
                TripId = entity.Id,
                BusId = entity.BusId,
                BusNumber = bus?.Number ?? string.Empty,
                PlateNumber = bus?.PlateNumber ?? string.Empty,
                DriverName = bus?.DriverName ?? string.Empty,
                TripType = (int)entity.Type,
                Date = entity.Date,
                AcademicYearId = yearId,
                StartedAt = startedAt,
                StudentIds = entity.Students.Select(s => s.StudentId).Distinct().ToList(),
                StartedBy = _currentUser.UserId
            });

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public class CompleteTripCommandHandler : ICommandHandler<CompleteTripCommand, bool>
    {
        private readonly ITripRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CompleteTripCommandHandler(ITripRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(CompleteTripCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            entity.Complete(DateTime.UtcNow);

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public class CancelTripCommandHandler : ICommandHandler<CancelTripCommand, bool>
    {
        private readonly ITripRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CancelTripCommandHandler(ITripRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(CancelTripCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            entity.Cancel();

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public class UpdateTripLocationCommandHandler : ICommandHandler<UpdateTripLocationCommand, bool>
    {
        private readonly ITripRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTripLocationCommandHandler(ITripRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateTripLocationCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            entity.UpdateLocation(command.Latitude, command.Longitude, DateTime.UtcNow);

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    /// <summary>Boarded / DroppedOff / Absent → the matching Trip method (Pending cannot be set)</summary>
    public class MarkTripStudentCommandHandler : ICommandHandler<MarkTripStudentCommand, bool>
    {
        private readonly ITripRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public MarkTripStudentCommandHandler(ITripRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(MarkTripStudentCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.TripId);
            if (entity is null) return false;

            var now = DateTime.UtcNow;
            switch (command.Status)
            {
                case TripStudentStatus.Boarded:
                    entity.MarkBoarded(command.StudentId, now);
                    break;
                case TripStudentStatus.DroppedOff:
                    entity.MarkDroppedOff(command.StudentId, now);
                    break;
                case TripStudentStatus.Absent:
                    entity.MarkAbsent(command.StudentId);
                    break;
                default:
                    throw new DomainException("A student can be marked boarded, dropped off or absent.");
            }

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
