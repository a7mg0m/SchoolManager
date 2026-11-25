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
        private readonly IImageFileStore _images;

        private readonly ICurrentUserService _currentUser;

        public UpdateNotificationCommandHandler(ICurrentUserService currentUser, INotificationRepository repository, IUnitOfWork unitOfWork, IImageFileStore images)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _images = images;
        }

        public async Task<Unit> Handle(UpdateNotificationCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"Notification {command.Id} not found");

            // The picture before the edit: deleted after saving when it was replaced / removed
            var oldImage = entity.ImageUrl;

            entity.Update(
                command.Title,
                command.Description,
                command.SentOn,
                command.NotificationType,
                command.ImageUrl,
                new AuditInfo(entity.AuditInfo.CreatedOn, entity.AuditInfo.CreatedBy, DateTime.Now, _currentUser.UserId),
                command.AcademicYearId == Guid.Empty ? null : command.AcademicYearId);

            if (command.Targets is not null)
            {
                entity.ClearTargets();
                foreach (var target in command.Targets)
                {
                    entity.AddTarget(target.GradeId, target.SectionId, target.TeacherId, target.StudentId,
                        new AuditInfo(DateTime.Now, _currentUser.UserId), target.BusId);
                }
            }

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            if (!string.Equals(oldImage, entity.ImageUrl, StringComparison.OrdinalIgnoreCase))
                _images.Delete(oldImage);

            return Unit.Value;
        }
    }
}
