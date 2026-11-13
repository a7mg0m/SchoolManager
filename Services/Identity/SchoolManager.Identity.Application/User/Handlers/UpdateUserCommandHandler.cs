using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, bool>
    {
        private readonly IUserRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public UpdateUserCommandHandler(ICurrentUserService currentUser, IUserRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _repository.GetByIdAsync(request.Id);

            if (user is null)
                return false;

            user.Update(
               
                request.LoginName,
                request.Email,
                request.RoleId,
                new AuditInfo(DateTime.Now, _currentUser.UserId)
            );

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
