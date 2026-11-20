using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// Assigns the students to the bus with the entered bus fee. Rules: the bus is active; the stop (if any) is on
    /// the bus's route; a student has only one active assignment per academic year; the bus's active assignments of
    /// the year plus the new ones fit its capacity. A BusFeeIntegrationEvent (Assigned) is queued for each
    /// assignment in the same SaveChanges.
    /// </summary>
    public class AssignStudentsToBusCommandHandler : ICommandHandler<AssignStudentsToBusCommand, List<Guid>>
    {
        private readonly IBusAssignmentRepository _repository;
        private readonly IBusRepository _busRepository;
        private readonly IBusRouteRepository _routeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIntegrationEventOutbox _outbox;
        private readonly ICurrentUserService _currentUser;

        public AssignStudentsToBusCommandHandler(
            ICurrentUserService currentUser,
            IBusAssignmentRepository repository,
            IBusRepository busRepository,
            IBusRouteRepository routeRepository,
            IUnitOfWork unitOfWork,
            IIntegrationEventOutbox outbox)
        {
            _currentUser = currentUser;
            _repository = repository;
            _busRepository = busRepository;
            _routeRepository = routeRepository;
            _unitOfWork = unitOfWork;
            _outbox = outbox;
        }

        public async Task<List<Guid>> Handle(AssignStudentsToBusCommand command, CancellationToken cancellationToken)
        {
            var studentIds = (command.StudentIds ?? new()).Where(id => id != Guid.Empty).Distinct().ToList();
            if (studentIds.Count == 0)
                throw new DomainException("Select at least one student.");
            if (command.FeeAmount <= 0)
                throw new DomainException("Bus fee must be greater than zero.");
            if (command.DueDate == default)
                throw new DomainException("Due date is required.");

            var bus = await BusAssignmentChecks.ActiveBusAsync(_busRepository, command.BusId);
            await BusAssignmentChecks.EnsureStopOnRouteAsync(_routeRepository, bus, command.BusRouteStopId);

            // One active assignment per student per academic year
            var yearId = command.AcademicYearId;
            var alreadyAssigned = await _repository.SearchAsync(a => studentIds.Contains(a.StudentId) && a.AcademicYearId == yearId && a.IsActive);
            if (alreadyAssigned.Count > 0)
                throw new DomainException($"{alreadyAssigned.Count} of the selected students already ride a bus in this academic year.");

            await BusAssignmentChecks.EnsureSeatsAsync(_repository, bus, yearId, studentIds.Count);

            var fee = new Money(command.FeeAmount, command.Currency);
            var now = DateTime.Now;
            var ids = new List<Guid>();
            foreach (var studentId in studentIds)
            {
                // A new AuditInfo per row: owned types are tracked by reference
                var assignment = BusAssignment.Create(studentId, bus.Id, yearId, command.BusRouteStopId, fee, command.StartDate,
                    new AuditInfo(now, _currentUser.UserId));
                await _repository.AddAsync(assignment);
                _outbox.Enqueue(Topics.AttendanceBusFees, assignment.Id.ToString(),
                    BusFeeEventFactory.From(assignment, BusFeeChangeType.Assigned, _currentUser.UserId, command.DueDate));
                ids.Add(assignment.Id);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ids;
        }
    }

    /// <summary>Checks shared by the assignment handlers</summary>
    internal static class BusAssignmentChecks
    {
        public static async Task<Bus> ActiveBusAsync(IBusRepository buses, Guid busId)
        {
            var bus = await buses.GetByIdAsync(busId)
                ?? throw new DomainException("Bus not found.");
            if (!bus.IsActive)
                throw new DomainException("The bus is not active.");
            return bus;
        }

        /// <summary>The stop must be one of the bus route's stops (no stop = not checked)</summary>
        public static async Task EnsureStopOnRouteAsync(IBusRouteRepository routes, Bus bus, Guid? busRouteStopId)
        {
            if (busRouteStopId is not Guid stopId || stopId == Guid.Empty)
                return;

            var route = await routes.GetByIdAsync(bus.BusRouteId);
            if (route is null || route.Stops.All(s => s.Id != stopId))
                throw new DomainException("The stop is not on the bus's route.");
        }

        /// <summary>The bus's active assignments of the academic year plus <paramref name="adding"/> fit its capacity</summary>
        public static async Task EnsureSeatsAsync(IBusAssignmentRepository assignments, Bus bus, Guid academicYearId, int adding)
        {
            var busId = bus.Id;
            var taken = (await assignments.SearchAsync(a => a.BusId == busId && a.AcademicYearId == academicYearId && a.IsActive)).Count;
            if (taken + adding > bus.Capacity)
                throw new DomainException($"The bus has {Math.Max(bus.Capacity - taken, 0)} free seats.");
        }
    }
}
