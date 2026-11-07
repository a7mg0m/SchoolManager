using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class UpdateRevenueCommandHandler : ICommandHandler<UpdateRevenueCommand, bool>
    {
        private readonly IRevenueRepository _repository;
        private readonly IRevenueCategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateRevenueCommandHandler(
            IRevenueRepository repository,
            IRevenueCategoryRepository categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateRevenueCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            var category = await _categoryRepository.GetByIdAsync(command.CategoryId)
                ?? throw new DomainException("Revenue category not found.");

            var audit = entity.AuditInfo;
            entity.Update(
                command.Title,
                command.Description,
                category,
                command.Type,
                new Money(command.Amount, command.Currency),
                command.RevenueDate,
                command.Reference,
                new AuditInfo(audit.CreatedOn, audit.CreatedBy, DateTime.Now, command.UpdatedBy));

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
