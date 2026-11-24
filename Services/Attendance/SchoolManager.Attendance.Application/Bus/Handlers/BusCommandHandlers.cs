using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// Creates the bus and queues a BusDriverIntegrationEvent (Created) in the same transaction;
    /// the Identity service then creates the driver's User and UserProfile.
    /// </summary>
    public class CreateBusCommandHandler : ICommandHandler<CreateBusCommand, Guid>
    {
        private readonly IBusRepository _repository;
        private readonly IBusRouteRepository _routeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIntegrationEventOutbox _outbox;
        private readonly ICurrentUserService _currentUser;

        public CreateBusCommandHandler(
            ICurrentUserService currentUser,
            IBusRepository repository,
            IBusRouteRepository routeRepository,
            IUnitOfWork unitOfWork,
            IIntegrationEventOutbox outbox)
        {
            _currentUser = currentUser;
            _repository = repository;
            _routeRepository = routeRepository;
            _unitOfWork = unitOfWork;
            _outbox = outbox;
        }

        public async Task<Guid> Handle(CreateBusCommand command, CancellationToken cancellationToken)
        {
            await BusChecks.EnsureRouteExistsAsync(_routeRepository, command.BusRouteId);
            await BusChecks.EnsureUniquePlateAsync(_repository, command.PlateNumber, null);

            var entity = Bus.Create(command.Number, command.PlateNumber, command.Capacity, command.DriverName, command.DriverPhone,
                command.BusRouteId, new AuditInfo(DateTime.Now, _currentUser.UserId));

            // Driver's login (created by the Identity service): login name kept here, password only as a hash
            entity.SetDriverLoginName(await BusDriverAccount.ValidateLoginNameAsync(command.DriverLoginName, entity.Id, _repository));
            var passwordHash = BusDriverAccount.HashPassword(command.DriverPassword, required: true);

            await _repository.AddAsync(entity);
            _outbox.Enqueue(Topics.AttendanceBusDrivers, entity.Id.ToString(),
                BusDriverAccount.Event(entity, BusDriverChangeType.Created, _currentUser.UserId, passwordHash));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }

    public class UpdateBusCommandHandler : ICommandHandler<UpdateBusCommand, bool>
    {
        private readonly IBusRepository _repository;
        private readonly IBusRouteRepository _routeRepository;
        private readonly IBusAssignmentRepository _assignmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIntegrationEventOutbox _outbox;
        private readonly ICurrentUserService _currentUser;

        public UpdateBusCommandHandler(
            ICurrentUserService currentUser,
            IBusRepository repository,
            IBusRouteRepository routeRepository,
            IBusAssignmentRepository assignmentRepository,
            IUnitOfWork unitOfWork,
            IIntegrationEventOutbox outbox)
        {
            _currentUser = currentUser;
            _repository = repository;
            _routeRepository = routeRepository;
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
            _outbox = outbox;
        }

        public async Task<bool> Handle(UpdateBusCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            await BusChecks.EnsureRouteExistsAsync(_routeRepository, command.BusRouteId);
            await BusChecks.EnsureUniquePlateAsync(_repository, command.PlateNumber, entity.Id);

            var busId = entity.Id;
            var activeCount = (await _assignmentRepository.SearchAsync(a => a.BusId == busId && a.IsActive)).Count;
            if (command.Capacity < activeCount)
                throw new DomainException($"The bus has {activeCount} students assigned; the capacity cannot be lower.");

            entity.Update(command.Number, command.PlateNumber, command.Capacity, command.DriverName, command.DriverPhone,
                command.BusRouteId, entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));

            // A bus without a driver login yet (saved before logins) needs a password now; otherwise empty keeps it
            var hasLogin = entity.DriverLoginName is not null;
            var loginName = await BusDriverAccount.ValidateLoginNameAsync(command.DriverLoginName, entity.Id, _repository);
            var passwordHash = BusDriverAccount.HashPassword(command.DriverPassword, required: !hasLogin);
            entity.SetDriverLoginName(loginName);

            await _repository.UpdateAsync(entity);
            _outbox.Enqueue(Topics.AttendanceBusDrivers, entity.Id.ToString(),
                BusDriverAccount.Event(entity, hasLogin ? BusDriverChangeType.Updated : BusDriverChangeType.Created, _currentUser.UserId, passwordHash));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public class DeleteBusCommandHandler : ICommandHandler<DeleteBusCommand, bool>
    {
        private readonly IBusRepository _repository;
        private readonly IBusAssignmentRepository _assignmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIntegrationEventOutbox _outbox;
        private readonly ICurrentUserService _currentUser;

        public DeleteBusCommandHandler(
            ICurrentUserService currentUser,
            IBusRepository repository,
            IBusAssignmentRepository assignmentRepository,
            IUnitOfWork unitOfWork,
            IIntegrationEventOutbox outbox)
        {
            _currentUser = currentUser;
            _repository = repository;
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
            _outbox = outbox;
        }

        public async Task<bool> Handle(DeleteBusCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            var busId = entity.Id;
            if ((await _assignmentRepository.SearchAsync(a => a.BusId == busId && a.IsActive)).Count > 0)
                throw new DomainException("The bus has students assigned. End or move their assignments first.");

            _repository.Remove(entity);
            // The driver's login is deleted too (Identity service)
            _outbox.Enqueue(Topics.AttendanceBusDrivers, entity.Id.ToString(), BusDriverAccount.Deleted(entity.Id, _currentUser.UserId));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    /// <summary>An inactive bus's driver cannot log in (the Identity service deactivates the user)</summary>
    public class SetBusActiveStatusCommandHandler : ICommandHandler<SetBusActiveStatusCommand, bool>
    {
        private readonly IBusRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIntegrationEventOutbox _outbox;
        private readonly ICurrentUserService _currentUser;

        public SetBusActiveStatusCommandHandler(
            ICurrentUserService currentUser, IBusRepository repository, IUnitOfWork unitOfWork, IIntegrationEventOutbox outbox)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _outbox = outbox;
        }

        public async Task<bool> Handle(SetBusActiveStatusCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            var auditInfo = entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId);
            if (command.IsActive)
                entity.Activate(auditInfo);
            else
                entity.Deactivate(auditInfo);

            await _repository.UpdateAsync(entity);
            if (entity.DriverLoginName is not null)
                _outbox.Enqueue(Topics.AttendanceBusDrivers, entity.Id.ToString(),
                    BusDriverAccount.Event(entity, BusDriverChangeType.Updated, _currentUser.UserId));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    /// <summary>Checks shared by the create and update handlers</summary>
    internal static class BusChecks
    {
        public static async Task EnsureRouteExistsAsync(IBusRouteRepository routes, Guid busRouteId)
        {
            if (await routes.GetByIdAsync(busRouteId) is null)
                throw new DomainException("Bus route not found.");
        }

        /// <summary>
        /// No other bus (than <paramref name="exceptBusId"/>) has this plate number (trimmed; the comparison runs in
        /// the database, so its collation decides upper / lower case)
        /// </summary>
        public static async Task EnsureUniquePlateAsync(IBusRepository buses, string plateNumber, Guid? exceptBusId)
        {
            var plate = (plateNumber ?? string.Empty).Trim();
            var same = await buses.SearchAsync(b => b.PlateNumber == plate);
            if (same.Any(b => b.Id != exceptBusId))
                throw new DomainException("Another bus has this plate number.");
        }
    }
}
