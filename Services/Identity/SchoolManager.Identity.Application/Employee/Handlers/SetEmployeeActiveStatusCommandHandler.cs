using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Activates/deactivates the employee and its User (a deactivated employee cannot log in).</summary>
    public class SetEmployeeActiveStatusCommandHandler : ICommandHandler<SetEmployeeActiveStatusCommand, bool>
    {
        private readonly IEmployeeRepository _repository;
        private readonly EmployeeAccountService _accounts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public SetEmployeeActiveStatusCommandHandler(
            IEmployeeRepository repository,
            EmployeeAccountService accounts,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _repository = repository;
            _accounts = accounts;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(SetEmployeeActiveStatusCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null)
                return false;

            var audit = entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId);
            if (command.IsActive)
                entity.Activate(audit);
            else
                entity.Deactivate(audit);

            await _accounts.SetActiveAsync(entity.UserId, command.IsActive);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
