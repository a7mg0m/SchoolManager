using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class CreateMenuItemCommandHandler : ICommandHandler<CreateMenuItemCommand, Guid>
    {
        private readonly IMenuItemRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateMenuItemCommandHandler(IMenuItemRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateMenuItemCommand command, CancellationToken cancellationToken)
        {
            if (command.ParentId is Guid parentId && await _repository.GetByIdAsync(parentId) is null)
                throw new DomainException(ErrorKeys.MenuItemParentNotFound);

            if (await _repository.NameExistsAsync(command.ParentId, command.NameAr, command.NameEn))
                throw new DomainException(ErrorKeys.MenuItemNameExists);

            var order = command.Order ?? await _repository.GetNextOrderAsync(command.ParentId);

            var entity = new MenuItem(
                new NameLocalized(command.NameAr.Trim(), command.NameEn.Trim()),
                MenuItemText.Optional(command.Icon),
                MenuItemText.Optional(command.Route),
                order,
                command.ParentId,
                command.IsActive,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}
