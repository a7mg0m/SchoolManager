using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class CreateExpenseCategoryCommandHandler : ICommandHandler<CreateExpenseCategoryCommand, Guid>
    {
        private readonly IExpenseCategoryRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public CreateExpenseCategoryCommandHandler(ICurrentUserService currentUser, IExpenseCategoryRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateExpenseCategoryCommand command, CancellationToken cancellationToken)
        {
            var entity = ExpenseCategory.Create(
                command.Name,
                command.Description,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
