

using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
    {
        private readonly IUserRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public CreateUserCommandHandler(IUserRepository repository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<Guid> Handle(CreateUserCommand command, CancellationToken cancellationToken)
        {
            var hashedPassword = _passwordHasher.Hash(command.HashPassword);

            var user = new User(
               
                command.LoginName,
                command.Email,
                hashedPassword,
                true,
                false,
                null,
                command.RoleId,
                new AuditInfo(DateTime.Now, command.CreatedBy)
            );

            await _repository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return user.Id;
        }
    }
}
