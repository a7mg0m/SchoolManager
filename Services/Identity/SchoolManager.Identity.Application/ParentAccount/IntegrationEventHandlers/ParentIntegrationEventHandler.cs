using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Keeps a parent's login User (role Parent, same id as the parent) and UserProfile (ParentId) in sync with
    /// the Management service, from Kafka events on the "management.parents" topic.
    ///
    /// - Created / Updated: create the User + UserProfile if missing, otherwise update them.
    ///   Login name and password are the ones entered on the admin parent page (the password arrives as a
    ///   BCrypt hash; none = keep the current one). Parents saved before that existed use the phone number
    ///   as login name and first password.
    ///   A parent without an email (or whose email is used by another user) gets a placeholder email,
    ///   because every user needs a unique email; a real email replaces it once entered.
    /// - Deleted: delete the User and its UserProfile.
    ///
    /// Messages may arrive more than once, so everything is idempotent. Problems that a retry cannot fix
    /// (no login name, login name already used by another user) are logged and the message is skipped.
    /// </summary>
    public class ParentIntegrationEventHandler : IIntegrationEventHandler<ParentIntegrationEvent>
    {
        private const string PlaceholderEmailDomain = "parents.local";

        private readonly IUserRepository _userRepository;
        private readonly IUserProfileRepository _profileRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ParentIntegrationEventHandler> _logger;

        public ParentIntegrationEventHandler(
            IUserRepository userRepository,
            IUserProfileRepository profileRepository,
            IRoleRepository roleRepository,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork,
            ILogger<ParentIntegrationEventHandler> logger)
        {
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleAsync(ParentIntegrationEvent evt, CancellationToken cancellationToken)
        {
            if (evt.ParentId == Guid.Empty)
            {
                _logger.LogWarning("Parent event {EventId} has no parent id, skipped", evt.EventId);
                return;
            }

            if (evt.ChangeType == ParentChangeType.Deleted)
                await DeleteAsync(evt);
            else
                await UpsertAsync(evt);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task UpsertAsync(ParentIntegrationEvent evt)
        {
            var now = DateTime.Now;
            var phone = evt.PhoneNumber?.Trim() ?? string.Empty;
            var (firstName, lastName) = PersonNames.Split(evt.FullNameEn, evt.FullNameAr);

            // Login name entered in Management; parents saved before it existed use the phone number
            var wantedLoginName = string.IsNullOrWhiteSpace(evt.LoginName) ? phone : evt.LoginName.Trim();
            var newPasswordHash = string.IsNullOrWhiteSpace(evt.PasswordHash) ? null : evt.PasswordHash;

            if (await _roleRepository.GetByIdAsync(RoleIds.Parent) is null)
                throw new InvalidOperationException("The Parent role is missing (seed data)"); // retried, then logged

            var user = await _userRepository.GetByIdAsync(evt.ParentId);
            var email = await ChooseEmailAsync(evt, user);

            if (user is null)
            {
                if (string.IsNullOrEmpty(wantedLoginName))
                {
                    _logger.LogError("Parent {ParentId}: no login created, a login name or phone number is required", evt.ParentId);
                    return;
                }
                if (await LoginNameUsedByOtherAsync(wantedLoginName, evt.ParentId))
                {
                    _logger.LogError("Parent {ParentId}: no login created, login name {LoginName} is already used", evt.ParentId, wantedLoginName);
                    return;
                }

                // First password: the one entered in Management (already hashed), else the phone number
                user = new User(
                    evt.ParentId,
                    wantedLoginName,
                    email,
                    newPasswordHash ?? _passwordHasher.Hash(phone),
                    true,
                    false,
                    null,
                    RoleIds.Parent,
                    new AuditInfo(now, evt.ChangedBy));
                await _userRepository.AddAsync(user);

                _logger.LogInformation("Parent {ParentId}: login {LoginName} created", evt.ParentId, wantedLoginName);
            }
            else
            {
                // Login name follows Management, unless another user already has it
                var loginName = user.LoginName;
                if (!string.IsNullOrEmpty(wantedLoginName) && wantedLoginName != user.LoginName)
                {
                    if (await LoginNameUsedByOtherAsync(wantedLoginName, user.Id))
                        _logger.LogWarning("Parent {ParentId}: login name {LoginName} is used by another user, keeping {CurrentLoginName}",
                            evt.ParentId, wantedLoginName, user.LoginName);
                    else
                        loginName = wantedLoginName;
                }

                var audit = user.AuditInfo.Updated(now, evt.ChangedBy);
                user.Update(loginName, email, RoleIds.Parent, audit);

                // A new password entered in Management (no hash = keep the current password)
                if (newPasswordHash is not null)
                    user.ChangePassword(newPasswordHash, audit);
            }

            // Profile: names from the English full name; image is kept (the parent sets it in My Profile)
            var profile = (await _profileRepository.SearchAsync(p => p.UserId == user.Id)).FirstOrDefault();
            if (profile is null)
            {
                profile = new UserProfile(
                    user.Id, firstName, lastName, email,
                    phone,
                    evt.Address?.Trim() ?? string.Empty,
                    Array.Empty<byte>(),
                    new AuditInfo(now, evt.ChangedBy));
                await _profileRepository.AddAsync(profile);
            }
            else
            {
                profile.Update(
                    firstName, lastName, email,
                    phone,
                    evt.Address?.Trim() ?? string.Empty,
                    profile.ProfileImage,
                    profile.AuditInfo.Updated(now, evt.ChangedBy));
            }

            // UserProfile.ParentId -> current parent id in the token
            profile.LinkParent(evt.ParentId);
        }

        /// <summary>
        /// - The parent has an email no other user has: that email.
        /// - The parent has an email another user already has: keep the login's current email, or a placeholder.
        /// - The parent has no email (none entered, or it was removed): a placeholder unique to the parent.
        /// </summary>
        private async Task<string> ChooseEmailAsync(ParentIntegrationEvent evt, User? user)
        {
            var email = evt.Email?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(email))
                return PlaceholderEmail(evt.ParentId);

            if (!await EmailUsedByOtherAsync(email, evt.ParentId))
                return email;

            _logger.LogWarning("Parent {ParentId}: email {Email} is used by another user", evt.ParentId, email);
            return user is not null && !string.IsNullOrEmpty(user.Email)
                ? user.Email
                : PlaceholderEmail(evt.ParentId);
        }

        private static string PlaceholderEmail(Guid parentId) => $"parent-{parentId:N}@{PlaceholderEmailDomain}";

        private async Task DeleteAsync(ParentIntegrationEvent evt)
        {
            var user = await _userRepository.GetByIdAsync(evt.ParentId);
            if (user is null)
                return; // already deleted (duplicate message) or never created

            if (user.IsReserved)
            {
                _logger.LogWarning("Parent {ParentId}: user is reserved and was not deleted", evt.ParentId);
                return;
            }

            foreach (var profile in await _profileRepository.SearchAsync(p => p.UserId == user.Id))
                _profileRepository.Remove(profile);

            _userRepository.Remove(user);
            _logger.LogInformation("Parent {ParentId}: login {LoginName} deleted", evt.ParentId, user.LoginName);
        }

        private async Task<bool> EmailUsedByOtherAsync(string email, Guid userId)
            => (await _userRepository.SearchAsync(u => u.Email == email)).Any(u => u.Id != userId);

        private async Task<bool> LoginNameUsedByOtherAsync(string loginName, Guid userId)
            => (await _userRepository.SearchAsync(u => u.LoginName == loginName)).Any(u => u.Id != userId);
    }
}
