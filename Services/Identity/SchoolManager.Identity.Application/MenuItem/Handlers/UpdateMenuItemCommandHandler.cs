using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class UpdateMenuItemCommandHandler : ICommandHandler<UpdateMenuItemCommand, bool>
    {
        private readonly IMenuItemRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public UpdateMenuItemCommandHandler(IMenuItemRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(UpdateMenuItemCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null)
                return false;

            if (command.ParentId is Guid parentId)
            {
                var all = await _repository.GetAllOrderedAsync();
                if (all.All(m => m.Id != parentId))
                    throw new DomainException(ErrorKeys.MenuItemParentNotFound);

                // An item cannot be its own parent, or be moved under one of its children
                if (MenuItemMapper.WouldCreateLoop(all, entity.Id, parentId))
                    throw new DomainException(ErrorKeys.MenuItemParentInvalid);
            }

            if (await _repository.NameExistsAsync(command.ParentId, command.NameAr, command.NameEn, command.Id))
                throw new DomainException(ErrorKeys.MenuItemNameExists);

            entity.Update(
                new NameLocalized(command.NameAr.Trim(), command.NameEn.Trim()),
                MenuItemText.Optional(command.Icon),
                MenuItemText.Optional(command.Route),
                command.Order,
                command.ParentId,
                entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
