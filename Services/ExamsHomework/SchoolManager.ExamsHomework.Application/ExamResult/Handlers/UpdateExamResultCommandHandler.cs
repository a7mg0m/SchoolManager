using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class UpdateExamResultCommandHandler : ICommandHandler<UpdateExamResultCommand>
    {
        private readonly IExamResultRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public UpdateExamResultCommandHandler(ICurrentUserService currentUser, IExamResultRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(UpdateExamResultCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"ExamResult {command.Id} not found");

            entity.UpdateScore(
                command.StudentId,
                command.Score,
                new AuditInfo(entity.AuditInfo.CreatedOn, entity.AuditInfo.CreatedBy, DateTime.Now, _currentUser.UserId));

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
