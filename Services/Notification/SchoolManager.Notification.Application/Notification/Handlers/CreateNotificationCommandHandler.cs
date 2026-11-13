using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class CreateNotificationCommandHandler : ICommandHandler<CreateNotificationCommand, Guid>
    {
        private readonly INotificationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public CreateNotificationCommandHandler(ICurrentUserService currentUser, INotificationRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
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
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            foreach (var target in command.Targets ?? [])
            {
                entity.AddTarget(target.GradeId, target.SectionId, target.TeacherId, target.StudentId,
                    new AuditInfo(DateTime.Now, _currentUser.UserId));
            }

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
