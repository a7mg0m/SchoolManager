using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class CreateEmployeeGroupCommandHandler : ICommandHandler<CreateEmployeeGroupCommand, Guid>
    {
        private readonly IEmployeeGroupRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateEmployeeGroupCommandHandler(IEmployeeGroupRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateEmployeeGroupCommand command, CancellationToken cancellationToken)
        {
            if (await _repository.NameExistsAsync(command.NameAr, command.NameEn))
                throw new DomainException(ErrorKeys.EmployeeGroupNameExists);

            var entity = new EmployeeGroup(
                new NameLocalized(command.NameAr.Trim(), command.NameEn.Trim()),
                EmployeeText.Optional(command.Description),
                command.IsActive,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}
