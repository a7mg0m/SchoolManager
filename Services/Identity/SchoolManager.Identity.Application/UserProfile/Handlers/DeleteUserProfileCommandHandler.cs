using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class DeleteUserProfileCommandHandler : ICommandHandler<DeleteUserProfileCommand, bool>
    {
        private readonly IUserProfileRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteUserProfileCommandHandler(IUserProfileRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteUserProfileCommand request, CancellationToken cancellationToken)
        {
            var profile = await _repository.GetByIdAsync(request.Id);

            if (profile is null)
                return false;

            _repository.Remove(profile);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
