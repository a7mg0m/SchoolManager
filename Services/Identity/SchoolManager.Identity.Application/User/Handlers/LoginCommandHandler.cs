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
        private readonly IParentStudentsChecker _parentStudents;
        private readonly ITeacherServiceChecker _teacherService;

        public LoginCommandHandler(
            IUserRepository repository,
            IUserProfileRepository profileRepository,
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IMapper mapper,
            IParentStudentsChecker parentStudents,
            ITeacherServiceChecker teacherService)
        {
            _parentStudents = parentStudents;
            _teacherService = teacherService;
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

            // A teacher signs in only while in service (checked first: ending service also deactivates the account,
            // and the teacher should be told why)
            if (user.RoleId == RoleIds.Teacher)
            {
                var teacherProfile = (await _profileRepository.SearchAsync(p => p.UserId == user.Id)).FirstOrDefault();
                var inService = await _teacherService.IsInServiceAsync(teacherProfile?.TeacherId ?? user.Id, cancellationToken);
                if (inService is null) throw new DomainException(ErrorKeys.TeacherCheckUnavailable);
                if (inService == false) throw new DomainException(ErrorKeys.ServiceEnded);
            }

            if (!user.IsActive)
            {
                throw new DomainException(ErrorKeys.UserInactive);
            }

            // One profile per user (unique index on UserProfiles.UserId)
            var profile = (await _profileRepository.SearchAsync(p => p.UserId == user.Id)).FirstOrDefault();

            // A parent signs in only while they have a child at the school (not all graduated / transferred)
            if (user.RoleId == RoleIds.Parent)
            {
                var hasCurrent = await _parentStudents.HasCurrentStudentsAsync(profile?.ParentId ?? user.Id, cancellationToken);
                if (hasCurrent is null) throw new DomainException(ErrorKeys.ParentCheckUnavailable);
                if (hasCurrent == false) throw new DomainException(ErrorKeys.NoCurrentStudents);
            }

            user.RecordLogin(DateTime.Now);
            await _unitOfWork.SaveChangesAsync();

            // Profile's employee / teacher / parent ids go into the token as claims
            var token = _jwtTokenGenerator.GenerateToken(user, profile);

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
