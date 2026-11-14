using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Updates a user profile (admin) and syncs the change to the User email and the linked Employee.
    /// </summary>
    public class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfileCommand, bool>
    {
        private readonly IUserProfileRepository _repository;
        private readonly ProfileSyncService _profileSync;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public UpdateUserProfileCommandHandler(
            ICurrentUserService currentUser,
            IUserProfileRepository repository,
            ProfileSyncService profileSync,
            IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _profileSync = profileSync;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
        {
            var profile = await _repository.GetByIdAsync(request.Id);

            if (profile is null)
                return false;

            var email = request.Email.Trim();
            await _profileSync.EnsureEmailAvailableAsync(profile.UserId, email);

            profile.Update(
                request.FirstName.Trim(),
                request.LastName.Trim(),
                email,
                request.PhoneNumber?.Trim() ?? string.Empty,
                request.Address?.Trim() ?? string.Empty,
                request.ProfileImage,
                // Keep the original CreatedOn / CreatedBy
                profile.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));

            await _profileSync.SyncAsync(profile);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
