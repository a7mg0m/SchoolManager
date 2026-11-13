using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class SetGradeActiveStatusCommandHandler : ICommandHandler<SetGradeActiveStatusCommand, bool>
    {
        private readonly IGradeRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public SetGradeActiveStatusCommandHandler(ICurrentUserService currentUser, IGradeRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(SetGradeActiveStatusCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);

            if (entity is null)
                return false;

            var auditInfo = new AuditInfo(DateTime.Now, _currentUser.UserId);

            if (request.IsActive)
                entity.Activate(auditInfo);
            else
                entity.Deactivate(auditInfo);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
