using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class CreateExpenseCommandHandler : ICommandHandler<CreateExpenseCommand, Guid>
    {
        private readonly IExpenseRepository _repository;
        private readonly IExpenseCategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public CreateExpenseCommandHandler(ICurrentUserService currentUser, 
            IExpenseRepository repository,
            IExpenseCategoryRepository categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateExpenseCommand command, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.GetByIdAsync(command.CategoryId)
                ?? throw new DomainException("Expense category not found.");

            var entity = Expense.Create(
                command.Title,
                command.Description,
                command.EventId,
                category,
                command.Type,
                new Money(command.Amount, command.Currency),
                command.ExpenseDate,
                command.Reference,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
