using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Saves the logged-in user's profile (creating it if missing) and syncs it to the
    /// User email and the linked Employee (name, email, phone, address). Everything is saved in one SaveChanges.
    /// </summary>
    public class UpdateMyProfileCommandHandler : ICommandHandler<UpdateMyProfileCommand, MyProfileDto>
    {
        private readonly MyProfileReader _reader;
        private readonly ProfileSyncService _profileSync;
        private readonly IUserProfileRepository _profileRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public UpdateMyProfileCommandHandler(
            MyProfileReader reader,
            ProfileSyncService profileSync,
            IUserProfileRepository profileRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _reader = reader;
            _profileSync = profileSync;
            _profileRepository = profileRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<MyProfileDto> Handle(UpdateMyProfileCommand command, CancellationToken cancellationToken)
        {
            var user = await _reader.GetCurrentUserAsync();

            var firstName = command.FirstName.Trim();
            var lastName = command.LastName.Trim();
            var email = command.Email.Trim();
            var phone = command.PhoneNumber?.Trim() ?? string.Empty;
            var address = command.Address?.Trim() ?? string.Empty;

            await _profileSync.EnsureEmailAvailableAsync(user.Id, email);

            var now = DateTime.Now;
            var profile = await _reader.GetProfileAsync(user.Id);

            var image = command.RemoveProfileImage
                ? Array.Empty<byte>()
                : command.ProfileImage is { Length: > 0 } newImage
                    ? newImage
                    : profile?.ProfileImage ?? Array.Empty<byte>();

            if (profile is null)
            {
                profile = new UserProfile(user.Id, firstName, lastName, email, phone, address, image,
                    new AuditInfo(now, _currentUser.UserId));
                await _profileRepository.AddAsync(profile);
            }
            else
            {
                profile.Update(firstName, lastName, email, phone, address, image,
                    profile.AuditInfo.Updated(now, _currentUser.UserId));
            }

            // User email + Employee name / email / phone / address
            var employee = await _profileSync.SyncAsync(profile, user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return await _reader.BuildAsync(user, profile, employee is not null);
        }
    }
}
