using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	/// <summary>
	/// Deletes the parent with its students and queues a ParentIntegrationEvent (Deleted) in the same transaction;
	/// the Identity service then deletes the parent's User and UserProfile.
	/// </summary>
	public class DeleteParentCommandHandler : ICommandHandler<DeleteParentCommand, bool>
	{
		private readonly IParentRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IStudentRepository _studentRepository;
		private readonly IIntegrationEventOutbox _outbox;
		private readonly ICurrentUserService _currentUser;

		public DeleteParentCommandHandler(IParentRepository repository, IStudentRepository studentRepository, IUnitOfWork unitOfWork, IIntegrationEventOutbox outbox, ICurrentUserService currentUser)
		{
			_studentRepository = studentRepository;
			_repository = repository;
			_unitOfWork = unitOfWork;
			_outbox = outbox;
			_currentUser = currentUser;
		}

		public async Task<bool> Handle(DeleteParentCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdWithStudentsAsync(request.Id);

			if (entity is null)
				return false;

			foreach (var student in entity.Students.ToList())
				_studentRepository.Remove(student);

			_repository.Remove(entity);

			_outbox.Enqueue(Topics.ManagementParents, entity.Id.ToString(),
				ParentEventFactory.Deleted(entity.Id, _currentUser.UserId));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
