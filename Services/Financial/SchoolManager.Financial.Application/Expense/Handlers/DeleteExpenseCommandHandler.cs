using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class DeleteExpenseCommandHandler : ICommandHandler<DeleteExpenseCommand, bool>
    {
        private readonly IExpenseRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteExpenseCommandHandler(IExpenseRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteExpenseCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
