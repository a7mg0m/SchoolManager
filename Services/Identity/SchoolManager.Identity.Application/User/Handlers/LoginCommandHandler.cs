using AutoMapper;
using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResultDto>
    {
        private readonly IUserRepository _repository;
        private readonly IUserProfileRepository _profileRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IMapper _mapper;

        public LoginCommandHandler(
            IUserRepository repository,
            IUserProfileRepository profileRepository,
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IMapper mapper)
        {
            _repository = repository;
            _profileRepository = profileRepository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _mapper = mapper;
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

            // One profile per user (unique index on UserProfiles.UserId)
            var profile = (await _profileRepository.SearchAsync(p => p.UserId == user.Id)).FirstOrDefault();

            return new LoginResultDto
            {
                UserId = user.Id,
                LoginName = user.LoginName,
                Token = token,
                UserProfile = profile is null ? null : _mapper.Map<UserProfileDto>(profile)
            };
        }
    }
}
