using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class UpdateEmployeeGroupCommandHandler : ICommandHandler<UpdateEmployeeGroupCommand, bool>
    {
        private readonly IEmployeeGroupRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public UpdateEmployeeGroupCommandHandler(IEmployeeGroupRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(UpdateEmployeeGroupCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null)
                return false;

            if (await _repository.NameExistsAsync(command.NameAr, command.NameEn, command.Id))
                throw new DomainException(ErrorKeys.EmployeeGroupNameExists);

            entity.Update(
                new NameLocalized(command.NameAr.Trim(), command.NameEn.Trim()),
                EmployeeText.Optional(command.Description),
                entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
