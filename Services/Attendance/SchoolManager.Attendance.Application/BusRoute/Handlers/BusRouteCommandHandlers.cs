using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class CreateBusRouteCommandHandler : ICommandHandler<CreateBusRouteCommand, Guid>
    {
        private readonly IBusRouteRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateBusRouteCommandHandler(ICurrentUserService currentUser, IBusRouteRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateBusRouteCommand command, CancellationToken cancellationToken)
        {
            var entity = BusRoute.Create(
                new NameLocalized(command.NameAr, command.NameEn),
                command.Description,
                new AuditInfo(DateTime.Now, _currentUser.UserId));
            entity.SetStops(BusRouteStops.From(command.Stops));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }

    public class UpdateBusRouteCommandHandler : ICommandHandler<UpdateBusRouteCommand, bool>
    {
        private readonly IBusRouteRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public UpdateBusRouteCommandHandler(ICurrentUserService currentUser, IBusRouteRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateBusRouteCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            entity.Update(
                new NameLocalized(command.NameAr, command.NameEn),
                command.Description,
                entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));
            entity.SetStops(BusRouteStops.From(command.Stops));

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public class DeleteBusRouteCommandHandler : ICommandHandler<DeleteBusRouteCommand, bool>
    {
        private readonly IBusRouteRepository _repository;
        private readonly IBusRepository _busRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteBusRouteCommandHandler(IBusRouteRepository repository, IBusRepository busRepository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _busRepository = busRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteBusRouteCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            var routeId = entity.Id;
            if ((await _busRepository.SearchAsync(b => b.BusRouteId == routeId)).Count > 0)
                throw new DomainException("The route is used by buses. Move the buses to another route or deactivate it.");

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public class SetBusRouteActiveStatusCommandHandler : ICommandHandler<SetBusRouteActiveStatusCommand, bool>
    {
        private readonly IBusRouteRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public SetBusRouteActiveStatusCommandHandler(ICurrentUserService currentUser, IBusRouteRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(SetBusRouteActiveStatusCommand command, CancellationToken cancellationToken)
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

    /// <summary>The stops of a create / update request, in the shape BusRoute.SetStops takes</summary>
    internal static class BusRouteStops
    {
        public static IEnumerable<(NameLocalized Name, int Order, double? Latitude, double? Longitude)> From(List<BusRouteStopDto>? stops)
            => (stops ?? new()).Select(s => (new NameLocalized(s.NameAr ?? string.Empty, s.NameEn ?? string.Empty), s.Order, s.Latitude, s.Longitude));
    }
}
