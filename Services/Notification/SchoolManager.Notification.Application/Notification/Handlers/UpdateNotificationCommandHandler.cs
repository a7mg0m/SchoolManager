using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class UpdateNotificationCommandHandler : ICommandHandler<UpdateNotificationCommand>
    {
        private readonly INotificationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public UpdateNotificationCommandHandler(ICurrentUserService currentUser, INotificationRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(UpdateNotificationCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"Notification {command.Id} not found");

            entity.Update(
                command.Title,
                command.Description,
                command.SentOn,
                command.NotificationType,
                command.ImageUrl,
                new AuditInfo(entity.AuditInfo.CreatedOn, entity.AuditInfo.CreatedBy, DateTime.Now, _currentUser.UserId));

            if (command.Targets is not null)
            {
                entity.ClearTargets();
                foreach (var target in command.Targets)
                {
                    entity.AddTarget(target.GradeId, target.SectionId, target.TeacherId, target.StudentId,
                        new AuditInfo(DateTime.Now, _currentUser.UserId));
                }
            }

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
