using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class CreateFeeCommandHandler : ICommandHandler<CreateFeeCommand, Guid>
    {
        private readonly IFeesRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public CreateFeeCommandHandler(ICurrentUserService currentUser, IFeesRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateFeeCommand command, CancellationToken cancellationToken)
        {
            var entity = Fee.Create(
                command.GradeId,
                command.Name,
                command.Type,
                new Money(command.Amount, command.Currency),
                command.IsMandatory,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
