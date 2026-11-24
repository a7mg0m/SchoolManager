using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Keeps a bus driver's login User (role Driver, same id as the bus) and UserProfile (BusId) in sync with
    /// the Attendance service, from Kafka events on the "attendance.bus-drivers" topic.
    ///
    /// - Created / Updated: create the User + UserProfile if missing, otherwise update them.
    ///   The login name and password come from the bus in Attendance (the password only as a hash).
    /// - Deleted: delete the User and its UserProfile.
    ///
    /// Messages may arrive more than once, so everything is idempotent. Problems that a retry cannot fix
    /// (login name already used by another user, no login name / password) are logged and the message is skipped.
    /// </summary>
    public class BusDriverIntegrationEventHandler : IIntegrationEventHandler<BusDriverIntegrationEvent>
    {
        /// <summary>Email for drivers (the user table needs an email; drivers have none)</summary>
        private const string PlaceholderEmailDomain = "drivers.local";

        private readonly IUserRepository _userRepository;
        private readonly IUserProfileRepository _profileRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BusDriverIntegrationEventHandler> _logger;

        public BusDriverIntegrationEventHandler(
            IUserRepository userRepository,
            IUserProfileRepository profileRepository,
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork,
            ILogger<BusDriverIntegrationEventHandler> logger)
        {
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleAsync(BusDriverIntegrationEvent evt, CancellationToken cancellationToken)
        {
            if (evt.BusId == Guid.Empty)
            {
                _logger.LogWarning("Bus driver event {EventId} has no bus id, skipped", evt.EventId);
                return;
            }

            if (evt.ChangeType == BusDriverChangeType.Deleted)
                await DeleteAsync(evt);
            else
                await UpsertAsync(evt);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task UpsertAsync(BusDriverIntegrationEvent evt)
        {
            var now = DateTime.Now;
            var (firstName, lastName) = PersonNames.Split(evt.DriverName, evt.DriverName);
            var wantedLoginName = evt.LoginName?.Trim() ?? string.Empty;
            var newPasswordHash = string.IsNullOrWhiteSpace(evt.PasswordHash) ? null : evt.PasswordHash;

            if (await _roleRepository.GetByIdAsync(RoleIds.Driver) is null)
                throw new InvalidOperationException("The Driver role is missing (seed data)"); // retried, then logged

            var user = await _userRepository.GetByIdAsync(evt.BusId);

            if (user is null)
            {
                if (string.IsNullOrEmpty(wantedLoginName) || newPasswordHash is null)
                {
                    _logger.LogError("Bus {BusId}: no driver login created, a login name and a password are required", evt.BusId);
                    return;
                }
                if (await LoginNameUsedByOtherAsync(wantedLoginName, evt.BusId))
                {
                    _logger.LogError("Bus {BusId}: no driver login created, login name {LoginName} is already used", evt.BusId, wantedLoginName);
                    return;
                }

                user = new User(
                    evt.BusId,
                    wantedLoginName,
                    PlaceholderEmail(evt.BusId),
                    newPasswordHash,
                    evt.IsActive,
                    false,
                    null,
                    RoleIds.Driver,
                    new AuditInfo(now, evt.ChangedBy));
                await _userRepository.AddAsync(user);

                _logger.LogInformation("Bus {BusId}: driver login {LoginName} created", evt.BusId, wantedLoginName);
            }
            else
            {
                // Login name follows the bus, unless another user already has it
                var loginName = user.LoginName;
                if (!string.IsNullOrEmpty(wantedLoginName) && wantedLoginName != user.LoginName)
                {
                    if (await LoginNameUsedByOtherAsync(wantedLoginName, user.Id))
                        _logger.LogWarning("Bus {BusId}: login name {LoginName} is used by another user, keeping {CurrentLoginName}",
                            evt.BusId, wantedLoginName, user.LoginName);
                    else
                        loginName = wantedLoginName;
                }

                var audit = user.AuditInfo.Updated(now, evt.ChangedBy);
                user.Update(loginName, user.Email, RoleIds.Driver, audit);

                if (newPasswordHash is not null)
                    user.ChangePassword(newPasswordHash, audit);

                if (evt.IsActive && !user.IsActive)
                    user.Activate(audit);
                else if (!evt.IsActive && user.IsActive)
                    user.Deactivate(audit);
            }

            // Profile: driver's name and phone; the image is kept (the driver sets it in My Profile)
            var profile = (await _profileRepository.SearchAsync(p => p.UserId == user.Id)).FirstOrDefault();
            if (profile is null)
            {
                profile = new UserProfile(
                    user.Id, firstName, lastName, user.Email,
                    evt.DriverPhone?.Trim() ?? string.Empty,
                    string.Empty,
                    Array.Empty<byte>(),
                    new AuditInfo(now, evt.ChangedBy));
                await _profileRepository.AddAsync(profile);
            }
            else
            {
                profile.Update(
                    firstName, lastName, profile.Email,
                    evt.DriverPhone?.Trim() ?? string.Empty,
                    profile.Address,
                    profile.ProfileImage,
                    profile.AuditInfo.Updated(now, evt.ChangedBy));
            }

            // UserProfile.BusId -> bus_id claim in the driver's token
            profile.LinkBus(evt.BusId);
        }

        private async Task DeleteAsync(BusDriverIntegrationEvent evt)
        {
            var user = await _userRepository.GetByIdAsync(evt.BusId);
            if (user is null)
                return; // already deleted (duplicate message) or never created

            if (user.IsReserved)
            {
                _logger.LogWarning("Bus {BusId}: user is reserved and was not deleted", evt.BusId);
                return;
            }

            foreach (var profile in await _profileRepository.SearchAsync(p => p.UserId == user.Id))
                _profileRepository.Remove(profile);

            _userRepository.Remove(user);
            _logger.LogInformation("Bus {BusId}: driver login {LoginName} deleted", evt.BusId, user.LoginName);
        }

        private static string PlaceholderEmail(Guid busId) => $"driver-{busId:N}@{PlaceholderEmailDomain}";

        private async Task<bool> LoginNameUsedByOtherAsync(string loginName, Guid userId)
            => (await _userRepository.SearchAsync(u => u.LoginName == loginName)).Any(u => u.Id != userId);
    }
}
