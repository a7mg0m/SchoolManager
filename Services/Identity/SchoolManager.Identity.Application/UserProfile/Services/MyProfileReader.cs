using BuildingBlocks;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Loads the logged-in user and builds <see cref="MyProfileDto"/>.</summary>
    public class MyProfileReader
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IUserRepository _userRepository;
        private readonly IUserProfileRepository _profileRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public MyProfileReader(
            ICurrentUserService currentUser,
            IUserRepository userRepository,
            IUserProfileRepository profileRepository,
            IRoleRepository roleRepository,
            IEmployeeRepository employeeRepository)
        {
            _currentUser = currentUser;
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _roleRepository = roleRepository;
            _employeeRepository = employeeRepository;
        }

        /// <summary>The logged-in user; throws when the request has no valid token or the user was deleted.</summary>
        public async Task<User> GetCurrentUserAsync()
        {
            if (!_currentUser.IsAuthenticated)
                throw new DomainException(ErrorKeys.UserNotAuthenticated);

            return await _userRepository.GetByIdAsync(_currentUser.UserId)
                ?? throw new DomainException(ErrorKeys.UserNotFound);
        }

        public async Task<UserProfile?> GetProfileAsync(Guid userId)
            => (await _profileRepository.SearchAsync(p => p.UserId == userId)).FirstOrDefault();

        public async Task<Employee?> GetEmployeeAsync(Guid userId)
            => (await _employeeRepository.SearchAsync(e => e.UserId == userId)).FirstOrDefault();

        public async Task<MyProfileDto> BuildAsync(User user, UserProfile? profile, bool isEmployee)
        {
            var role = await _roleRepository.GetByIdAsync(user.RoleId);

            return new MyProfileDto
            {
                Id = profile?.Id,
                UserId = user.Id,
                LoginName = user.LoginName,
                RoleNameAr = role?.Name.Arabic,
                RoleNameEn = role?.Name.English,
                LastLogin = user.LastLogin,
                IsEmployee = isEmployee,
                FirstName = profile?.FirstName ?? string.Empty,
                LastName = profile?.LastName ?? string.Empty,
                // A user without a profile still has the email of the account
                Email = profile?.Email ?? user.Email,
                PhoneNumber = profile?.PhoneNumber,
                Address = profile?.Address,
                ProfileImage = profile?.ProfileImage is { Length: > 0 } image ? image : null
            };
        }
    }
}
