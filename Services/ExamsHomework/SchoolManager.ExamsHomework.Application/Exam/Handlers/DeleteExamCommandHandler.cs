using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class DeleteExamCommandHandler : ICommandHandler<DeleteExamCommand>
    {
        private readonly IExamRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteExamCommandHandler(IExamRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteExamCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"Exam {command.Id} not found");

            // Its second final must be deleted first
            if ((await _repository.SearchAsync(e => e.OriginalExamId == entity.Id)).Count > 0)
                throw new ExamRuleException(SecondFinal.HasSecondFinal);

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
