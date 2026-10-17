using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class UpdateHomeworkCommandHandler : ICommandHandler<UpdateHomeworkCommand>
    {
        private readonly IHomeworkRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateHomeworkCommandHandler(IHomeworkRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(UpdateHomeworkCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"Homework {command.Id} not found");

            entity.Update(
                command.Title,
                command.Description,
                command.DueDate,
                command.AttachmentUrl,
                new AuditInfo(entity.AuditInfo.CreatedOn, entity.AuditInfo.CreatedBy, DateTime.Now, command.UpdatedBy));

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
