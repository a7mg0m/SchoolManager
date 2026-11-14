using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	/// <summary>
	/// Creates the teacher and queues a TeacherIntegrationEvent (Created) in the same transaction;
	/// the Identity service then creates the teacher's User and UserProfile.
	/// </summary>
	public class CreateTeacherCommandHandler : ICommandHandler<CreateTeacherCommand, Guid>
	{
		private readonly ITeacherRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IIntegrationEventOutbox _outbox;

		private readonly ICurrentUserService _currentUser;

		public CreateTeacherCommandHandler(
			ICurrentUserService currentUser,
			ITeacherRepository repository,
			IUnitOfWork unitOfWork,
			IIntegrationEventOutbox outbox)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
			_outbox = outbox;
		}

		public async Task<Guid> Handle(CreateTeacherCommand command, CancellationToken cancellationToken)
		{
			var entity = new Teacher(
				new NameLocalized(command.FullNameAr.Trim(), command.FullNameEn.Trim()),
                command.DateOfBirth,
                command.NationalId.Trim(),
                command.Gender,
                command.PhoneNumber.Trim(),
                command.Email?.Trim(),
                command.Address,
                command.IsActive,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _repository.AddAsync(entity);

			_outbox.Enqueue(Topics.ManagementTeachers, entity.Id.ToString(),
				TeacherEventFactory.From(entity, TeacherChangeType.Created, _currentUser.UserId));

			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
