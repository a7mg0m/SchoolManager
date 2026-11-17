using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class CreateUserProfileCommandHandler : ICommandHandler<CreateUserProfileCommand, Guid>
    {
        private readonly IUserProfileRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ProfileSyncService _profileSync;

        private readonly ICurrentUserService _currentUser;

        public CreateUserProfileCommandHandler(ICurrentUserService currentUser, IUserProfileRepository repository, IUnitOfWork unitOfWork, ProfileSyncService profileSync)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _profileSync = profileSync;
        }

        public async Task<Guid> Handle(CreateUserProfileCommand command, CancellationToken cancellationToken)
        {
            var profile = new UserProfile(
                command.UserId,
                command.FirstName,
                command.LastName,
                command.Email,
                command.PhoneNumber,
                command.Address,
                command.ProfileImage,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            // Employee id of the user (teacher ids come from the teacher sync)
            await _profileSync.LinkAsync(profile);

            await _repository.AddAsync(profile);
            await _unitOfWork.SaveChangesAsync();

            return profile.Id;
        }
    }
}
