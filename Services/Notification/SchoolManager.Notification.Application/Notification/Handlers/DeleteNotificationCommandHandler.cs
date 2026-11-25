using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class DeleteNotificationCommandHandler : ICommandHandler<DeleteNotificationCommand>
    {
        private readonly INotificationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageFileStore _images;

        public DeleteNotificationCommandHandler(INotificationRepository repository, IUnitOfWork unitOfWork, IImageFileStore images)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _images = images;
        }

        public async Task<Unit> Handle(DeleteNotificationCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"Notification {command.Id} not found");

            var image = entity.ImageUrl;
            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            // Its uploaded picture goes too
            _images.Delete(image);

            return Unit.Value;
        }
    }
}
