using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Updates the employee and syncs its User (login name, email, role Admin, optional new password) and UserProfile.</summary>
    public class UpdateEmployeeCommandHandler : ICommandHandler<UpdateEmployeeCommand, bool>
    {
        private readonly IEmployeeRepository _repository;
        private readonly IEmployeeGroupRepository _groupRepository;
        private readonly EmployeeAccountService _accounts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public UpdateEmployeeCommandHandler(
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

        public async Task<bool> Handle(UpdateEmployeeCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null)
                return false;

            // Moving to another group requires that group to be active; staying in a
            // group that was deactivated later is allowed.
            if (command.EmployeeGroupId != entity.EmployeeGroupId)
            {
                var group = await _groupRepository.GetByIdAsync(command.EmployeeGroupId)
                    ?? throw new DomainException(ErrorKeys.EmployeeGroupNotFound);
                if (!group.IsActive)
                    throw new DomainException(ErrorKeys.EmployeeGroupInactive);
            }

            if (await _repository.NationalIdExistsAsync(command.NationalId, command.Id))
                throw new DomainException(ErrorKeys.NationalIdExists);

            var data = EmployeeAccountData.From(command.FullNameEn, command.Email, command.PhoneNumber, command.Address, command.LoginName);
            var user = await _accounts.UpdateOrCreateAsync(entity.UserId, data, command.Password, entity.IsActive);

            entity.Update(
                new NameLocalized(command.FullNameAr.Trim(), command.FullNameEn.Trim()),
                command.NationalId.Trim(),
                data.PhoneNumber,
                data.Email,
                EmployeeText.Optional(command.Address),
                command.JobTitle.Trim(),
                command.HireDate,
                command.EmployeeGroupId,
                user.Id,
                entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
