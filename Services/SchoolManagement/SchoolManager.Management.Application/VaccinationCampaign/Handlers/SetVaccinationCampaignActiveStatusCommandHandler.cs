using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class SetVaccinationCampaignActiveStatusCommandHandler : ICommandHandler<SetVaccinationCampaignActiveStatusCommand, bool>
    {
        private readonly IVaccinationCampaignRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public SetVaccinationCampaignActiveStatusCommandHandler(ICurrentUserService currentUser, IVaccinationCampaignRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(SetVaccinationCampaignActiveStatusCommand request, CancellationToken cancellationToken)
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
