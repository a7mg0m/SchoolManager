using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class DeleteHomeworkQuestionCommandHandler : ICommandHandler<DeleteHomeworkQuestionCommand>
    {
        private readonly IHomeworkQuestionRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteHomeworkQuestionCommandHandler(IHomeworkQuestionRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteHomeworkQuestionCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"HomeworkQuestion {command.Id} not found");

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
