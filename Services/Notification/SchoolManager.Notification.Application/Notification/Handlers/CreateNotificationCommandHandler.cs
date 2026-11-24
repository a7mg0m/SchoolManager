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
        private readonly INotificationPushService _push;

        public CreateNotificationCommandHandler(
            ICurrentUserService currentUser,
            INotificationRepository repository,
            IUnitOfWork unitOfWork,
            INotificationPushService push)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _push = push;
        }

        public async Task<Guid> Handle(CreateNotificationCommand command, CancellationToken cancellationToken)
        {
            var entity = new Domain.Notification(
                command.Title,
                command.Description,
                command.SentOn,
                command.NotificationType,
                command.ImageUrl,
                new AuditInfo(DateTime.Now, _currentUser.UserId),
                command.AcademicYearId);

            foreach (var target in command.Targets ?? [])
            {
                entity.AddTarget(target.GradeId, target.SectionId, target.TeacherId, target.StudentId,
                    new AuditInfo(DateTime.Now, _currentUser.UserId), target.BusId);
            }

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            // Push to the recipients' phones now when the notification is due (SentOn today or earlier).
            // A future SentOn is pushed on that day by DuePushNotificationsService. Push problems are logged by
            // the push service and never fail the request; the notification itself is already saved.
            if (entity.SentOn <= DateOnly.FromDateTime(DateTime.Now))
            {
                await _push.PushNotificationAsync(entity, cancellationToken);
                if (entity.PushSentAt is not null)
                    await _unitOfWork.SaveChangesAsync();
            }

            return entity.Id;
        }
    }
}
