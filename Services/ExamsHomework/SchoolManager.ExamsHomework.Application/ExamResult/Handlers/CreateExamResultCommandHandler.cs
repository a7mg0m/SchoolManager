using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class CreateExamResultCommandHandler : ICommandHandler<CreateExamResultCommand, Guid>
    {
        private readonly IExamResultRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateExamResultCommandHandler(IExamResultRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateExamResultCommand command, CancellationToken cancellationToken)
        {
            var entity = new ExamResult(
                command.ExamId,
                command.StudentId,
                command.Score,
                new AuditInfo(DateTime.Now, command.CreatedBy));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
