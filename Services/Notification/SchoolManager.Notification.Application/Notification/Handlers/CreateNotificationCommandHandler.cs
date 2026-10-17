using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class CreateNotificationCommandHandler : ICommandHandler<CreateNotificationCommand, Guid>
    {
        private readonly INotificationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateNotificationCommandHandler(INotificationRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateNotificationCommand command, CancellationToken cancellationToken)
        {
            var entity = new Domain.Notification(
                command.Title,
                command.Description,
                command.SentOn,
                command.NotificationType,
                command.ImageUrl,
                new AuditInfo(DateTime.Now, command.CreatedBy));

            foreach (var target in command.Targets ?? [])
            {
                entity.AddTarget(target.GradeId, target.SectionId, target.TeacherId, target.StudentId,
                    new AuditInfo(DateTime.Now, command.CreatedBy));
            }

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
