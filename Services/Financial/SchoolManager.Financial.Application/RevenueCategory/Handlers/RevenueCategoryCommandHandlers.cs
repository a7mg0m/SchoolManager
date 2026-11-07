using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class CreateRevenueCategoryCommandHandler : ICommandHandler<CreateRevenueCategoryCommand, Guid>
    {
        private readonly IRevenueCategoryRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateRevenueCategoryCommandHandler(IRevenueCategoryRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateRevenueCategoryCommand command, CancellationToken cancellationToken)
        {
            var entity = RevenueCategory.Create(
                command.Name,
                command.Description,
                new AuditInfo(DateTime.Now, command.CreatedBy));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }

    public class UpdateRevenueCategoryCommandHandler : ICommandHandler<UpdateRevenueCategoryCommand, bool>
    {
        private readonly IRevenueCategoryRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateRevenueCategoryCommandHandler(IRevenueCategoryRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateRevenueCategoryCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            var audit = entity.AuditInfo;
            entity.Update(
                command.Name,
                command.Description,
                new AuditInfo(audit.CreatedOn, audit.CreatedBy, DateTime.Now, command.UpdatedBy));

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }

    public class DeleteRevenueCategoryCommandHandler : ICommandHandler<DeleteRevenueCategoryCommand, bool>
    {
        private readonly IRevenueCategoryRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteRevenueCategoryCommandHandler(IRevenueCategoryRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteRevenueCategoryCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
