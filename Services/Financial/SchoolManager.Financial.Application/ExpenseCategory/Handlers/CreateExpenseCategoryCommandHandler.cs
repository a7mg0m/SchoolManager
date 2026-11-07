using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class CreateExpenseCategoryCommandHandler : ICommandHandler<CreateExpenseCategoryCommand, Guid>
    {
        private readonly IExpenseCategoryRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateExpenseCategoryCommandHandler(IExpenseCategoryRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateExpenseCategoryCommand command, CancellationToken cancellationToken)
        {
            var entity = ExpenseCategory.Create(
                command.Name,
                command.Description,
                new AuditInfo(DateTime.Now, command.CreatedBy));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
