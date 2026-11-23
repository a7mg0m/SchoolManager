using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	/// <summary>
	/// Updates the teacher and queues a TeacherIntegrationEvent (Updated) in the same transaction;
	/// the Identity service then updates the teacher's User and UserProfile.
	/// </summary>
	public class UpdateTeacherCommandHandler : ICommandHandler<UpdateTeacherCommand, bool>
	{
		private readonly ITeacherRepository _repository;
		private readonly IParentRepository _parents;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IIntegrationEventOutbox _outbox;

		private readonly ICurrentUserService _currentUser;

		public UpdateTeacherCommandHandler(
			ICurrentUserService currentUser,
			ITeacherRepository repository,
			IParentRepository parents,
			IUnitOfWork unitOfWork,
			IIntegrationEventOutbox outbox)
		{
			_currentUser = currentUser;
			_repository = repository;
			_parents = parents;
			_unitOfWork = unitOfWork;
			_outbox = outbox;
		}

		public async Task<bool> Handle(UpdateTeacherCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			// Login account (kept in sync by the Identity service): an empty password keeps the current one
			var loginName = await TeacherAccount.ValidateLoginNameAsync(request.LoginName, entity.Id, _repository, _parents);
			var passwordHash = TeacherAccount.HashPassword(request.Password, required: false);
			entity.SetLoginName(loginName);
			entity.SetJobLevel(request.JobLevel);

			entity.Update(
				new NameLocalized(request.FullNameAr.Trim(), request.FullNameEn.Trim()),
                request.PhoneNumber.Trim(),
                request.Email?.Trim(),
                request.Address,
                request.IsActive,
                // Keep the original CreatedOn / CreatedBy
                entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));

			_outbox.Enqueue(Topics.ManagementTeachers, entity.Id.ToString(),
				TeacherEventFactory.From(entity, TeacherChangeType.Updated, _currentUser.UserId, passwordHash));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
