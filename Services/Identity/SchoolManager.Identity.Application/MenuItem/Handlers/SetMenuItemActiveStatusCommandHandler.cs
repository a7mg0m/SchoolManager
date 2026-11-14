using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Activates / deactivates one item. Children keep their own status, but the active menu tree
    /// hides everything under an inactive item.
    /// </summary>
    public class SetMenuItemActiveStatusCommandHandler : ICommandHandler<SetMenuItemActiveStatusCommand, bool>
    {
        private readonly IMenuItemRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public SetMenuItemActiveStatusCommandHandler(IMenuItemRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(SetMenuItemActiveStatusCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null)
                return false;

            var audit = entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId);
            if (command.IsActive)
                entity.Activate(audit);
            else
                entity.Deactivate(audit);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
