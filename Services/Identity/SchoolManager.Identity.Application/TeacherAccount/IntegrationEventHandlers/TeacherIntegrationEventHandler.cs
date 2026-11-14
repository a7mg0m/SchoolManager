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
    ///   A new login uses the National ID as login name and first password (the teacher changes it in My Profile).
    /// - Deleted: delete the User and its UserProfile.
    ///
    /// Messages may arrive more than once, so everything is idempotent. Problems that a retry cannot fix
    /// (login name or email already used by another user) are logged and the message is skipped.
    /// </summary>
    public class TeacherIntegrationEventHandler : IIntegrationEventHandler<TeacherIntegrationEvent>
    {
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
            var email = evt.Email?.Trim() ?? string.Empty;
            var nationalId = evt.NationalId.Trim();
            var (firstName, lastName) = SplitName(evt.FullNameEn, evt.FullNameAr);

            if (await _roleRepository.GetByIdAsync(RoleIds.Teacher) is null)
                throw new InvalidOperationException("The Teacher role is missing (seed data)"); // retried, then logged

            var user = await _userRepository.GetByIdAsync(evt.TeacherId);

            if (user is null)
            {
                // New login: login name and first password = National ID
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(nationalId))
                {
                    _logger.LogError("Teacher {TeacherId}: no login created, email and national id are required", evt.TeacherId);
                    return;
                }
                if (await _userRepository.GetByLoginNameAsync(nationalId) is not null)
                {
                    _logger.LogError("Teacher {TeacherId}: no login created, login name {LoginName} is already used", evt.TeacherId, nationalId);
                    return;
                }
                if (await EmailUsedByOtherAsync(email, evt.TeacherId))
                {
                    _logger.LogError("Teacher {TeacherId}: no login created, email {Email} is already used", evt.TeacherId, email);
                    return;
                }

                user = new User(
                    evt.TeacherId,
                    nationalId,
                    email,
                    _passwordHasher.Hash(nationalId),
                    evt.IsActive,
                    false,
                    null,
                    RoleIds.Teacher,
                    new AuditInfo(now, evt.ChangedBy));
                await _userRepository.AddAsync(user);

                _logger.LogInformation("Teacher {TeacherId}: login {LoginName} created", evt.TeacherId, nationalId);
            }
            else
            {
                // Keep the current email when the new one is empty or used by another user
                if (!string.IsNullOrEmpty(email) && await EmailUsedByOtherAsync(email, user.Id))
                {
                    _logger.LogWarning("Teacher {TeacherId}: email {Email} is used by another user, keeping {CurrentEmail}",
                        evt.TeacherId, email, user.Email);
                    email = user.Email;
                }
                if (string.IsNullOrEmpty(email))
                    email = user.Email;

                var audit = user.AuditInfo.Updated(now, evt.ChangedBy);
                user.Update(user.LoginName, email, RoleIds.Teacher, audit);

                if (evt.IsActive && !user.IsActive)
                    user.Activate(audit);
                else if (!evt.IsActive && user.IsActive)
                    user.Deactivate(audit);
            }

            // Profile: names from the English full name; image is kept (the teacher sets it in My Profile)
            var profile = (await _profileRepository.SearchAsync(p => p.UserId == user.Id)).FirstOrDefault();
            if (profile is null)
            {
                await _profileRepository.AddAsync(new UserProfile(
                    user.Id, firstName, lastName, email,
                    evt.PhoneNumber?.Trim() ?? string.Empty,
                    evt.Address?.Trim() ?? string.Empty,
                    Array.Empty<byte>(),
                    new AuditInfo(now, evt.ChangedBy)));
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

        private async Task<bool> EmailUsedByOtherAsync(string email, Guid userId)
            => (await _userRepository.SearchAsync(u => u.Email == email)).Any(u => u.Id != userId);

        /// <summary>"Ahmed Ali Hassan" -> ("Ahmed", "Ali Hassan"); falls back to the Arabic name.</summary>
        private static (string First, string Last) SplitName(string fullNameEn, string fullNameAr)
        {
            var source = string.IsNullOrWhiteSpace(fullNameEn) ? fullNameAr : fullNameEn;
            var parts = (source ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return ("-", "-");

            var first = parts[0];
            var last = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : parts[0];
            return (Truncate(first), Truncate(last));
        }

        private static string Truncate(string value) => value.Length <= 100 ? value : value[..100];
    }
}
