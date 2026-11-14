using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class DeleteMenuItemCommandHandler : ICommandHandler<DeleteMenuItemCommand, bool>
    {
        private readonly IMenuItemRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteMenuItemCommandHandler(IMenuItemRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteMenuItemCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null)
                return false;

            if (await _repository.HasChildrenAsync(command.Id))
                throw new DomainException(ErrorKeys.MenuItemHasChildren);

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
