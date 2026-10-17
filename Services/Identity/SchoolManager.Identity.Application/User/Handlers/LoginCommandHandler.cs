using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResultDto>
    {
        private readonly IUserRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public LoginCommandHandler(
            IUserRepository repository,
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _repository.GetByLoginNameAsync(request.LoginName);

            if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new DomainException(ErrorKeys.InvalidCredentials);
            }

            if (!user.IsActive)
            {
                throw new DomainException(ErrorKeys.UserInactive);
            }

            user.RecordLogin(DateTime.Now);
            await _unitOfWork.SaveChangesAsync();

            var token = _jwtTokenGenerator.GenerateToken(user);

            return new LoginResultDto
            {
                UserId = user.Id,
                LoginName = user.LoginName,
                Token = token
            };
        }
    }
}
