using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class DeleteSchoolEventCommandHandler : ICommandHandler<DeleteSchoolEventCommand>
    {
        private readonly ISchoolEventRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageFileStore _images;

        public DeleteSchoolEventCommandHandler(ISchoolEventRepository repository, IUnitOfWork unitOfWork, IImageFileStore images)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _images = images;
        }

        public async Task<Unit> Handle(DeleteSchoolEventCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"SchoolEvent {command.Id} not found");

            var image = entity.ImageUrl;
            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            // Its uploaded picture goes too
            _images.Delete(image);

            return Unit.Value;
        }
    }
}
