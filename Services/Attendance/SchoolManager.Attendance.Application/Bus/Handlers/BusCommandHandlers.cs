using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class CreateBusCommandHandler : ICommandHandler<CreateBusCommand, Guid>
    {
        private readonly IBusRepository _repository;
        private readonly IBusRouteRepository _routeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateBusCommandHandler(
            ICurrentUserService currentUser,
            IBusRepository repository,
            IBusRouteRepository routeRepository,
            IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _routeRepository = routeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateBusCommand command, CancellationToken cancellationToken)
        {
            await BusChecks.EnsureRouteExistsAsync(_routeRepository, command.BusRouteId);
            await BusChecks.EnsureUniquePlateAsync(_repository, command.PlateNumber, null);

            var entity = Bus.Create(command.Number, command.PlateNumber, command.Capacity, command.DriverName, command.DriverPhone,
                command.BusRouteId, new AuditInfo(DateTime.Now, _currentUser.UserId));

            await _repository.AddAsync(entity);
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
        private readonly ICurrentUserService _currentUser;

        public UpdateBusCommandHandler(
            ICurrentUserService currentUser,
            IBusRepository repository,
            IBusRouteRepository routeRepository,
            IBusAssignmentRepository assignmentRepository,
            IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _routeRepository = routeRepository;
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
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

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public class DeleteBusCommandHandler : ICommandHandler<DeleteBusCommand, bool>
    {
        private readonly IBusRepository _repository;
        private readonly IBusAssignmentRepository _assignmentRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteBusCommandHandler(IBusRepository repository, IBusAssignmentRepository assignmentRepository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteBusCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            var busId = entity.Id;
            if ((await _assignmentRepository.SearchAsync(a => a.BusId == busId && a.IsActive)).Count > 0)
                throw new DomainException("The bus has students assigned. End or move their assignments first.");

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public class SetBusActiveStatusCommandHandler : ICommandHandler<SetBusActiveStatusCommand, bool>
    {
        private readonly IBusRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public SetBusActiveStatusCommandHandler(ICurrentUserService currentUser, IBusRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
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
