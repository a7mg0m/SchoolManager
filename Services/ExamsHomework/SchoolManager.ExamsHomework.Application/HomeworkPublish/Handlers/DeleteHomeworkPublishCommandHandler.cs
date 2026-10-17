using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class DeleteHomeworkPublishCommandHandler : ICommandHandler<DeleteHomeworkPublishCommand>
    {
        private readonly IHomeworkPublishRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteHomeworkPublishCommandHandler(IHomeworkPublishRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteHomeworkPublishCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"HomeworkPublish {command.Id} not found");

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
