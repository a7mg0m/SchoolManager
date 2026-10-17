using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfileCommand, bool>
    {
        private readonly IUserProfileRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateUserProfileCommandHandler(IUserProfileRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
        {
            var profile = await _repository.GetByIdAsync(request.Id);

            if (profile is null)
                return false;

            profile.Update(
                request.FirstName,
                request.LastName,
                request.Email,
                request.PhoneNumber,
                request.Address,
                request.ProfileImage,
                new AuditInfo(DateTime.Now, request.UpdatedBy));

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
