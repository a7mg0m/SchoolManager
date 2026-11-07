using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class DeleteRevenueCommandHandler : ICommandHandler<DeleteRevenueCommand, bool>
    {
        private readonly IRevenueRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteRevenueCommandHandler(IRevenueRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteRevenueCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
