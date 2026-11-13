using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class CreateRevenueCommandHandler : ICommandHandler<CreateRevenueCommand, Guid>
    {
        private readonly IRevenueRepository _repository;
        private readonly IRevenueCategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public CreateRevenueCommandHandler(ICurrentUserService currentUser, 
            IRevenueRepository repository,
            IRevenueCategoryRepository categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateRevenueCommand command, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.GetByIdAsync(command.CategoryId)
                ?? throw new DomainException("Revenue category not found.");

            var entity = Revenue.Create(
                command.Title,
                command.Description,
                category,
                command.Type,
                new Money(command.Amount, command.Currency),
                command.RevenueDate,
                command.Reference,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
