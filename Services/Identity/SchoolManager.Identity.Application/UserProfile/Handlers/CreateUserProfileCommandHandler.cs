using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class CreateUserProfileCommandHandler : ICommandHandler<CreateUserProfileCommand, Guid>
    {
        private readonly IUserProfileRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public CreateUserProfileCommandHandler(ICurrentUserService currentUser, IUserProfileRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
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

            await _repository.AddAsync(profile);
            await _unitOfWork.SaveChangesAsync();

            return profile.Id;
        }
    }
}
