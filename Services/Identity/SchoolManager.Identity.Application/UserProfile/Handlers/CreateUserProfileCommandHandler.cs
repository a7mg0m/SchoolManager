using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class CreateUserProfileCommandHandler : ICommandHandler<CreateUserProfileCommand, Guid>
    {
        private readonly IUserProfileRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateUserProfileCommandHandler(IUserProfileRepository repository, IUnitOfWork unitOfWork)
        {
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
                new AuditInfo(DateTime.Now, command.CreatedBy));

            await _repository.AddAsync(profile);
            await _unitOfWork.SaveChangesAsync();

            return profile.Id;
        }
    }
}
