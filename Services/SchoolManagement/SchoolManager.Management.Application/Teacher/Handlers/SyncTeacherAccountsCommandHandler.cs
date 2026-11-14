using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class SyncTeacherAccountsCommandHandler : ICommandHandler<SyncTeacherAccountsCommand, int>
	{
		private readonly ITeacherRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IIntegrationEventOutbox _outbox;
		private readonly ICurrentUserService _currentUser;

		public SyncTeacherAccountsCommandHandler(
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

		public async Task<int> Handle(SyncTeacherAccountsCommand command, CancellationToken cancellationToken)
		{
			var teachers = await _repository.GetAllAsync();

			// Identity treats Updated as "create or update", so this is safe to run any number of times
			foreach (var teacher in teachers)
			{
				_outbox.Enqueue(Topics.ManagementTeachers, teacher.Id.ToString(),
					TeacherEventFactory.From(teacher, TeacherChangeType.Updated, _currentUser.UserId));
			}

			await _unitOfWork.SaveChangesAsync();
			return teachers.Count;
		}
	}
}
