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

        public DeleteSchoolEventCommandHandler(ISchoolEventRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteSchoolEventCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"SchoolEvent {command.Id} not found");

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
