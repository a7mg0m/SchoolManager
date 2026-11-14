using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Creates the employee, its login User (role Admin) and its UserProfile.
    /// Everything is saved in one SaveChanges call, so it is all-or-nothing.
    /// </summary>
    public class CreateEmployeeCommandHandler : ICommandHandler<CreateEmployeeCommand, Guid>
    {
        private readonly IEmployeeRepository _repository;
        private readonly IEmployeeGroupRepository _groupRepository;
        private readonly EmployeeAccountService _accounts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateEmployeeCommandHandler(
            IEmployeeRepository repository,
            IEmployeeGroupRepository groupRepository,
            EmployeeAccountService accounts,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _repository = repository;
            _groupRepository = groupRepository;
            _accounts = accounts;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateEmployeeCommand command, CancellationToken cancellationToken)
        {
            var group = await _groupRepository.GetByIdAsync(command.EmployeeGroupId)
                ?? throw new DomainException(ErrorKeys.EmployeeGroupNotFound);
            if (!group.IsActive)
                throw new DomainException(ErrorKeys.EmployeeGroupInactive);

            if (await _repository.NationalIdExistsAsync(command.NationalId))
                throw new DomainException(ErrorKeys.NationalIdExists);

            var data = EmployeeAccountData.From(command.FullNameEn, command.Email, command.PhoneNumber, command.Address, command.LoginName);
            var user = await _accounts.CreateAsync(data, command.Password, command.IsActive);

            var entity = new Employee(
                new NameLocalized(command.FullNameAr.Trim(), command.FullNameEn.Trim()),
                command.NationalId.Trim(),
                data.PhoneNumber,
                data.Email,
                EmployeeText.Optional(command.Address),
                command.JobTitle.Trim(),
                command.HireDate,
                command.EmployeeGroupId,
                user.Id,
                command.IsActive,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}
