using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class DeleteTimetableSlotCommandHandler : ICommandHandler<DeleteTimetableSlotCommand, bool>
	{
		private readonly ITimetableSlotRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public DeleteTimetableSlotCommandHandler(ITimetableSlotRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(DeleteTimetableSlotCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			_repository.Remove(entity);

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
