using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class SyncParentAccountsCommandHandler : ICommandHandler<SyncParentAccountsCommand, int>
	{
		private readonly IParentRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IIntegrationEventOutbox _outbox;
		private readonly ICurrentUserService _currentUser;

		public SyncParentAccountsCommandHandler(
			IParentRepository repository,
			IUnitOfWork unitOfWork,
			IIntegrationEventOutbox outbox,
			ICurrentUserService currentUser)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
			_outbox = outbox;
			_currentUser = currentUser;
		}

		public async Task<int> Handle(SyncParentAccountsCommand command, CancellationToken cancellationToken)
		{
			var parents = await _repository.GetAllAsync();

			// Identity treats Updated as "create or update", so this is safe to run any number of times
			foreach (var parent in parents)
			{
				_outbox.Enqueue(Topics.ManagementParents, parent.Id.ToString(),
					ParentEventFactory.From(parent, ParentChangeType.Updated, _currentUser.UserId));
			}

			await _unitOfWork.SaveChangesAsync();
			return parents.Count;
		}
	}
}
