using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Keeps a teacher's login User (role Teacher, same id as the teacher) and UserProfile in sync with
    /// the Management service, from Kafka events on the "management.teachers" topic.
    ///
    /// - Created / Updated: create the User + UserProfile if missing, otherwise update them.
    ///   The login name and password come from the teacher page in Management (the password only as a hash);
    ///   teachers saved before that use the National ID as login name and first password.
    /// - Deleted: delete the User and its UserProfile.
    ///
    /// Messages may arrive more than once, so everything is idempotent. Problems that a retry cannot fix
    /// (login name or email already used by another user) are logged and the message is skipped.
    /// </summary>
    public class TeacherIntegrationEventHandler : IIntegrationEventHandler<TeacherIntegrationEvent>
    {
        /// <summary>Email for teachers without one (the user table needs an email)</summary>
        private const string PlaceholderEmailDomain = "teachers.local";

        private readonly IUserRepository _userRepository;
        private readonly IUserProfileRepository _profileRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TeacherIntegrationEventHandler> _logger;

        public TeacherIntegrationEventHandler(
            IUserRepository userRepository,
            IUserProfileRepository profileRepository,
            IRoleRepository roleRepository,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork,
            ILogger<TeacherIntegrationEventHandler> logger)
        {
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleAsync(TeacherIntegrationEvent evt, CancellationToken cancellationToken)
        {
            if (evt.TeacherId == Guid.Empty)
            {
                _logger.LogWarning("Teacher event {EventId} has no teacher id, skipped", evt.EventId);
                return;
            }

            if (evt.ChangeType == TeacherChangeType.Deleted)
                await DeleteAsync(evt);
            else
                await UpsertAsync(evt);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task UpsertAsync(TeacherIntegrationEvent evt)
        {
            var now = DateTime.Now;
            var nationalId = evt.NationalId.Trim();
            var (firstName, lastName) = PersonNames.Split(evt.FullNameEn, evt.FullNameAr);

            // Login name entered in Management; teachers saved before it existed use the National ID
            var wantedLoginName = string.IsNullOrWhiteSpace(evt.LoginName) ? nationalId : evt.LoginName.Trim();
            var newPasswordHash = string.IsNullOrWhiteSpace(evt.PasswordHash) ? null : evt.PasswordHash;

            if (await _roleRepository.GetByIdAsync(RoleIds.Teacher) is null)
                throw new InvalidOperationException("The Teacher role is missing (seed data)"); // retried, then logged

            var user = await _userRepository.GetByIdAsync(evt.TeacherId);
            var email = await ChooseEmailAsync(evt, user);

            if (user is null)
            {
                if (string.IsNullOrEmpty(wantedLoginName))
                {
                    _logger.LogError("Teacher {TeacherId}: no login created, a login name or national id is required", evt.TeacherId);
                    return;
                }
                if (await LoginNameUsedByOtherAsync(wantedLoginName, evt.TeacherId))
                {
                    _logger.LogError("Teacher {TeacherId}: no login created, login name {LoginName} is already used", evt.TeacherId, wantedLoginName);
                    return;
                }

                // First password: the one entered in Management (already hashed), else the National ID
                user = new User(
                    evt.TeacherId,
                    wantedLoginName,
                    email,
                    newPasswordHash ?? _passwordHasher.Hash(nationalId),
                    evt.IsActive,
                    false,
                    null,
                    RoleIds.Teacher,
                    new AuditInfo(now, evt.ChangedBy));
                await _userRepository.AddAsync(user);

                _logger.LogInformation("Teacher {TeacherId}: login {LoginName} created", evt.TeacherId, wantedLoginName);
            }
            else
            {
                // Login name follows Management, unless another user already has it
                var loginName = user.LoginName;
                if (!string.IsNullOrEmpty(wantedLoginName) && wantedLoginName != user.LoginName)
                {
                    if (await LoginNameUsedByOtherAsync(wantedLoginName, user.Id))
                        _logger.LogWarning("Teacher {TeacherId}: login name {LoginName} is used by another user, keeping {CurrentLoginName}",
                            evt.TeacherId, wantedLoginName, user.LoginName);
                    else
                        loginName = wantedLoginName;
                }

                var audit = user.AuditInfo.Updated(now, evt.ChangedBy);
                user.Update(loginName, email, RoleIds.Teacher, audit);

                // A new password entered in Management (no hash = keep the current password)
                if (newPasswordHash is not null)
                    user.ChangePassword(newPasswordHash, audit);

                if (evt.IsActive && !user.IsActive)
                    user.Activate(audit);
                else if (!evt.IsActive && user.IsActive)
                    user.Deactivate(audit);
            }

            // Profile: names from the English full name; image is kept (the teacher sets it in My Profile)
            var profile = (await _profileRepository.SearchAsync(p => p.UserId == user.Id)).FirstOrDefault();
            if (profile is null)
            {
                profile = new UserProfile(
                    user.Id, firstName, lastName, email,
                    evt.PhoneNumber?.Trim() ?? string.Empty,
                    evt.Address?.Trim() ?? string.Empty,
                    Array.Empty<byte>(),
                    new AuditInfo(now, evt.ChangedBy));
                await _profileRepository.AddAsync(profile);
            }
            else
            {
                profile.Update(
                    firstName, lastName, email,
                    evt.PhoneNumber?.Trim() ?? string.Empty,
                    evt.Address?.Trim() ?? string.Empty,
                    profile.ProfileImage,
                    profile.AuditInfo.Updated(now, evt.ChangedBy));
            }

            // UserProfile.TeacherId -> current teacher id in the token
            profile.LinkTeacher(evt.TeacherId);
        }

        private async Task DeleteAsync(TeacherIntegrationEvent evt)
        {
            var user = await _userRepository.GetByIdAsync(evt.TeacherId);
            if (user is null)
                return; // already deleted (duplicate message) or never created

            if (user.IsReserved)
            {
                _logger.LogWarning("Teacher {TeacherId}: user is reserved and was not deleted", evt.TeacherId);
                return;
            }

            foreach (var profile in await _profileRepository.SearchAsync(p => p.UserId == user.Id))
                _profileRepository.Remove(profile);

            _userRepository.Remove(user);
            _logger.LogInformation("Teacher {TeacherId}: login {LoginName} deleted", evt.TeacherId, user.LoginName);
        }

        /// <summary>
        /// The teacher's email; a placeholder when it is empty (teachers may have none), or the current one
        /// when another user already has it.
        /// </summary>
        private async Task<string> ChooseEmailAsync(TeacherIntegrationEvent evt, User? user)
        {
            var email = evt.Email?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(email))
                return user is not null && !string.IsNullOrEmpty(user.Email) ? user.Email : PlaceholderEmail(evt.TeacherId);

            if (!await EmailUsedByOtherAsync(email, evt.TeacherId))
                return email;

            _logger.LogWarning("Teacher {TeacherId}: email {Email} is used by another user", evt.TeacherId, email);
            return user is not null && !string.IsNullOrEmpty(user.Email)
                ? user.Email
                : PlaceholderEmail(evt.TeacherId);
        }

        private static string PlaceholderEmail(Guid teacherId) => $"teacher-{teacherId:N}@{PlaceholderEmailDomain}";

        private async Task<bool> LoginNameUsedByOtherAsync(string loginName, Guid userId)
            => (await _userRepository.SearchAsync(u => u.LoginName == loginName)).Any(u => u.Id != userId);

        private async Task<bool> EmailUsedByOtherAsync(string email, Guid userId)
            => (await _userRepository.SearchAsync(u => u.Email == email)).Any(u => u.Id != userId);
    }
}
