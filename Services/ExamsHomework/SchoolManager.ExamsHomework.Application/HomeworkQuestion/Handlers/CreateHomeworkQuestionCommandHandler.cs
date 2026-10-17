using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class CreateHomeworkQuestionCommandHandler : ICommandHandler<CreateHomeworkQuestionCommand, Guid>
    {
        private readonly IHomeworkQuestionRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateHomeworkQuestionCommandHandler(IHomeworkQuestionRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateHomeworkQuestionCommand command, CancellationToken cancellationToken)
        {
            var entity = new HomeworkQuestion(
                command.HomeworkId,
                command.QuestionHeader,
                command.Questions,
                command.ImageUrl,
                new AuditInfo(DateTime.Now, command.CreatedBy));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
