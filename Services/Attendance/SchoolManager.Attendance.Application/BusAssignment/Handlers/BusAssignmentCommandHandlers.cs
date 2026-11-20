using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>New bus fee of an active assignment; Financial is told (FeeChanged) in the same SaveChanges</summary>
    public class ChangeBusFeeCommandHandler : ICommandHandler<ChangeBusFeeCommand, bool>
    {
        private readonly IBusAssignmentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIntegrationEventOutbox _outbox;
        private readonly ICurrentUserService _currentUser;

        public ChangeBusFeeCommandHandler(
            ICurrentUserService currentUser,
            IBusAssignmentRepository repository,
            IUnitOfWork unitOfWork,
            IIntegrationEventOutbox outbox)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _outbox = outbox;
        }

        public async Task<bool> Handle(ChangeBusFeeCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;
            if (!entity.IsActive)
                throw new DomainException("The assignment has ended.");

            entity.ChangeFee(new Money(command.FeeAmount, command.Currency), entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));

            await _repository.UpdateAsync(entity);
            _outbox.Enqueue(Topics.AttendanceBusFees, entity.Id.ToString(),
                BusFeeEventFactory.From(entity, BusFeeChangeType.FeeChanged, _currentUser.UserId));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    /// <summary>
    /// Moves the student to another active bus (and stop on its route). Moving to another bus needs a free seat
    /// there in the same academic year. The fee does not change.
    /// </summary>
    public class MoveStudentToBusCommandHandler : ICommandHandler<MoveStudentToBusCommand, bool>
    {
        private readonly IBusAssignmentRepository _repository;
        private readonly IBusRepository _busRepository;
        private readonly IBusRouteRepository _routeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public MoveStudentToBusCommandHandler(
            ICurrentUserService currentUser,
            IBusAssignmentRepository repository,
            IBusRepository busRepository,
            IBusRouteRepository routeRepository,
            IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _busRepository = busRepository;
            _routeRepository = routeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(MoveStudentToBusCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            var bus = await BusAssignmentChecks.ActiveBusAsync(_busRepository, command.BusId);
            await BusAssignmentChecks.EnsureStopOnRouteAsync(_routeRepository, bus, command.BusRouteStopId);
            if (bus.Id != entity.BusId)
                await BusAssignmentChecks.EnsureSeatsAsync(_repository, bus, entity.AcademicYearId, 1);

            entity.MoveToBus(bus.Id, command.BusRouteStopId, entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    /// <summary>Ends the assignment; Financial is told (Ended) in the same SaveChanges</summary>
    public class EndBusAssignmentCommandHandler : ICommandHandler<EndBusAssignmentCommand, bool>
    {
        private readonly IBusAssignmentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIntegrationEventOutbox _outbox;
        private readonly ICurrentUserService _currentUser;

        public EndBusAssignmentCommandHandler(
            ICurrentUserService currentUser,
            IBusAssignmentRepository repository,
            IUnitOfWork unitOfWork,
            IIntegrationEventOutbox outbox)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _outbox = outbox;
        }

        public async Task<bool> Handle(EndBusAssignmentCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            entity.End(command.EndDate, entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));

            await _repository.UpdateAsync(entity);
            _outbox.Enqueue(Topics.AttendanceBusFees, entity.Id.ToString(),
                BusFeeEventFactory.From(entity, BusFeeChangeType.Ended, _currentUser.UserId));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
