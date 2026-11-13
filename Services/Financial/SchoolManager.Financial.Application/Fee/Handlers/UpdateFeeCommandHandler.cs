using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class UpdateFeeCommandHandler : ICommandHandler<UpdateFeeCommand, bool>
    {
        private readonly IFeesRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public UpdateFeeCommandHandler(ICurrentUserService currentUser, IFeesRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateFeeCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            var audit = entity.AuditInfo;
            entity.Update(
                command.Name,
                command.Type,
                new Money(command.Amount, command.Currency),
                command.IsMandatory,
                new AuditInfo(audit.CreatedOn, audit.CreatedBy, DateTime.Now, _currentUser.UserId));

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
