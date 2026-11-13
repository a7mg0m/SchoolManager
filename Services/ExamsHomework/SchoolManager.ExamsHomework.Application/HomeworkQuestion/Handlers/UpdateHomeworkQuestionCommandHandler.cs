using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class UpdateHomeworkQuestionCommandHandler : ICommandHandler<UpdateHomeworkQuestionCommand>
    {
        private readonly IHomeworkQuestionRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public UpdateHomeworkQuestionCommandHandler(ICurrentUserService currentUser, IHomeworkQuestionRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(UpdateHomeworkQuestionCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"HomeworkQuestion {command.Id} not found");

            entity.Update(
                command.QuestionHeader,
                command.Questions,
                command.ImageUrl,
                new AuditInfo(entity.AuditInfo.CreatedOn, entity.AuditInfo.CreatedBy, DateTime.Now, _currentUser.UserId));

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
