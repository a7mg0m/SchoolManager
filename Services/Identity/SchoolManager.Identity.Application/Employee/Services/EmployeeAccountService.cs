using BuildingBlocks;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Keeps the login User (role Admin) and UserProfile of an employee in sync with the employee.
    /// It only stages changes; the calling handler saves everything in one SaveChanges.
    /// </summary>
    public class EmployeeAccountService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserProfileRepository _profileRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICurrentUserService _currentUser;

        public EmployeeAccountService(
            IUserRepository userRepository,
            IUserProfileRepository profileRepository,
            IRoleRepository roleRepository,
            IPasswordHasher passwordHasher,
            ICurrentUserService currentUser)
        {
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
            _currentUser = currentUser;
        }

        /// <summary>Creates a User with role Admin and its UserProfile.</summary>
        public async Task<User> CreateAsync(EmployeeAccountData data, string? password, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new DomainException(ErrorKeys.PasswordRequired);

            await EnsureAdminRoleAsync();
            await EnsureUniqueAsync(data, excludeUserId: null);

            var now = DateTime.Now;
            var user = new User(
                data.LoginName,
                data.Email,
                _passwordHasher.Hash(password),
                isActive,
                false,
                null,
                RoleIds.Admin,
                new AuditInfo(now, _currentUser.UserId));
            await _userRepository.AddAsync(user);

            var profile = new UserProfile(
                user.Id,
                data.FirstName,
                data.LastName,
                data.Email,
                data.PhoneNumber,
                data.Address,
                Array.Empty<byte>(),
                new AuditInfo(now, _currentUser.UserId));
            await _profileRepository.AddAsync(profile);

            return user;
        }

        /// <summary>
        /// Updates the employee's User and UserProfile. If the employee has no User yet
        /// (or it was deleted), a new one is created, which needs a password.
        /// </summary>
        public async Task<User> UpdateOrCreateAsync(Guid? userId, EmployeeAccountData data, string? newPassword, bool isActive)
        {
            var user = userId is Guid id ? await _userRepository.GetByIdAsync(id) : null;
            if (user is null)
                return await CreateAsync(data, newPassword, isActive);

            await EnsureAdminRoleAsync();
            await EnsureUniqueAsync(data, excludeUserId: user.Id);

            var now = DateTime.Now;
            var userAudit = user.AuditInfo.Updated(now, _currentUser.UserId);
            user.Update(data.LoginName, data.Email, RoleIds.Admin, userAudit);
            if (!string.IsNullOrWhiteSpace(newPassword))
                user.ChangePassword(_passwordHasher.Hash(newPassword), userAudit);

            var profile = (await _profileRepository.SearchAsync(p => p.UserId == user.Id)).FirstOrDefault();
            if (profile is null)
            {
                await _profileRepository.AddAsync(new UserProfile(
                    user.Id,
                    data.FirstName,
                    data.LastName,
                    data.Email,
                    data.PhoneNumber,
                    data.Address,
                    Array.Empty<byte>(),
                    new AuditInfo(now, _currentUser.UserId)));
            }
            else
            {
                profile.Update(
                    data.FirstName,
                    data.LastName,
                    data.Email,
                    data.PhoneNumber,
                    data.Address,
                    profile.ProfileImage,   // keep the uploaded image
                    profile.AuditInfo.Updated(now, _currentUser.UserId));
            }

            return user;
        }

        public async Task SetActiveAsync(Guid? userId, bool isActive)
        {
            if (userId is not Guid id || await _userRepository.GetByIdAsync(id) is not User user)
                return;

            var audit = user.AuditInfo.Updated(DateTime.Now, _currentUser.UserId);
            if (isActive)
                user.Activate(audit);
            else
                user.Deactivate(audit);
        }

        /// <summary>Removes the User and its UserProfile. Reserved (system) users are never deleted.</summary>
        public async Task DeleteAsync(Guid? userId)
        {
            if (userId is not Guid id || await _userRepository.GetByIdAsync(id) is not User user || user.IsReserved)
                return;

            foreach (var profile in await _profileRepository.SearchAsync(p => p.UserId == user.Id))
                _profileRepository.Remove(profile);

            _userRepository.Remove(user);
        }

        private async Task EnsureAdminRoleAsync()
        {
            if (await _roleRepository.GetByIdAsync(RoleIds.Admin) is null)
                throw new DomainException(ErrorKeys.AdminRoleNotFound);
        }

        private async Task EnsureUniqueAsync(EmployeeAccountData data, Guid? excludeUserId)
        {
            var byLogin = await _userRepository.SearchAsync(u => u.LoginName == data.LoginName);
            if (byLogin.Any(u => u.Id != excludeUserId))
                throw new DomainException(ErrorKeys.LoginNameExists);

            var byEmail = await _userRepository.SearchAsync(u => u.Email == data.Email);
            if (byEmail.Any(u => u.Id != excludeUserId))
                throw new DomainException(ErrorKeys.EmailExists);
        }
    }
}
