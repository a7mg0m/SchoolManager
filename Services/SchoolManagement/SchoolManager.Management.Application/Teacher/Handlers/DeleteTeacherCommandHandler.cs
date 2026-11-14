using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	/// <summary>
	/// Deletes the teacher and queues a TeacherIntegrationEvent (Deleted) in the same transaction;
	/// the Identity service then deletes the teacher's User and UserProfile.
	/// </summary>
	public class DeleteTeacherCommandHandler : ICommandHandler<DeleteTeacherCommand, bool>
	{
		private readonly ITeacherRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IIntegrationEventOutbox _outbox;
		private readonly ICurrentUserService _currentUser;

		public DeleteTeacherCommandHandler(
			ITeacherRepository repository,
			IUnitOfWork unitOfWork,
			IIntegrationEventOutbox outbox,
			ICurrentUserService currentUser)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
			_outbox = outbox;
			_currentUser = currentUser;
		}

		public async Task<bool> Handle(DeleteTeacherCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			_repository.Remove(entity);

			_outbox.Enqueue(Topics.ManagementTeachers, entity.Id.ToString(),
				TeacherEventFactory.Deleted(entity.Id, _currentUser.UserId));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
