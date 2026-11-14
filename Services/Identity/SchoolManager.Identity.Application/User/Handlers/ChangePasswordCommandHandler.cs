using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, bool>
    {
        private readonly IUserRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICurrentUserService _currentUser;

        public ChangePasswordCommandHandler(
            IUserRepository repository,
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            ICurrentUserService currentUser)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated)
                throw new DomainException(ErrorKeys.UserNotAuthenticated);

            var user = await _repository.GetByIdAsync(_currentUser.UserId)
                ?? throw new DomainException(ErrorKeys.UserNotFound);

            if (!_passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
                throw new DomainException(ErrorKeys.CurrentPasswordInvalid);

            if (command.CurrentPassword == command.NewPassword)
                throw new DomainException(ErrorKeys.NewPasswordSameAsCurrent);

            user.ChangePassword(
                _passwordHasher.Hash(command.NewPassword),
                user.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
