using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class UpdateExpenseCommandHandler : ICommandHandler<UpdateExpenseCommand, bool>
    {
        private readonly IExpenseRepository _repository;
        private readonly IExpenseCategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateExpenseCommandHandler(
            IExpenseRepository repository,
            IExpenseCategoryRepository categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateExpenseCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            var category = await _categoryRepository.GetByIdAsync(command.CategoryId)
                ?? throw new DomainException("Expense category not found.");

            var audit = entity.AuditInfo;
            entity.Update(
                command.Title,
                command.Description,
                command.EventId,
                category,
                command.Type,
                new Money(command.Amount, command.Currency),
                command.ExpenseDate,
                command.Reference,
                new AuditInfo(audit.CreatedOn, audit.CreatedBy, DateTime.Now, command.UpdatedBy));

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
