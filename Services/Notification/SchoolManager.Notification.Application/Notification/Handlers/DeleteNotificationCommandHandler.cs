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

        public DeleteNotificationCommandHandler(INotificationRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteNotificationCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"Notification {command.Id} not found");

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
