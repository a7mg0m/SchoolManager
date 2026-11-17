using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	/// <summary>
	/// Creates the parent with its students and queues a ParentIntegrationEvent (Created) in the same transaction;
	/// the Identity service then creates the parent's User and UserProfile.
	/// </summary>
	public class CreateParentCommandHandler : ICommandHandler<CreateParentCommand, Guid>
	{
		private readonly IParentRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IGradeRepository _gradeRepository;
		private readonly IStudentRepository _studentRepository;
		private readonly IIntegrationEventOutbox _outbox;

		private readonly ICurrentUserService _currentUser;

		public CreateParentCommandHandler(ICurrentUserService currentUser, IParentRepository repository, IStudentRepository studentRepository, IGradeRepository gradeRepository, IUnitOfWork unitOfWork, IIntegrationEventOutbox outbox)
		{
			_currentUser = currentUser;
			_gradeRepository = gradeRepository;
			_studentRepository = studentRepository;
			_repository = repository;
			_unitOfWork = unitOfWork;
			_outbox = outbox;
		}

		public async Task<Guid> Handle(CreateParentCommand command, CancellationToken cancellationToken)
		{
			var existingIds = await _studentRepository.GetExistingNationalIdsAsync(
				command.Students.Select(s => s.NationalId), cancellationToken);
			if (existingIds.Count > 0)
				throw new DuplicateNationalIdException(existingIds);

			var entity = new Parent(
				new NameLocalized(command.FullNameAr, command.FullNameEn),
                command.PhoneNumber,
                command.Email,
                command.Address,
				new AuditInfo(DateTime.Now, _currentUser.UserId));

			// Login account (created by the Identity service): login name kept here, password only as a hash
			entity.SetLoginName(await ParentAccount.ValidateLoginNameAsync(command.LoginName, entity.Id, _repository));
			var passwordHash = ParentAccount.HashPassword(command.Password, required: true);

			foreach (var s in command.Students)
			{
				var grade = await _gradeRepository.GetByIdAsync(s.GradeId)
					?? throw new InvalidOperationException("Grade not found.");

				entity.Students.Add(new Student(
					new NameLocalized(s.FullNameAr, s.FullNameEn),
					s.DateOfBirth,
					s.NationalId,
					s.Gender,
					s.PhoneNumber,
					s.Email,
					s.Address,
					grade.StageId,
					s.GradeId,
					s.SectionId,
					s.AcademicYearId,
					entity.Id,
					new AuditInfo(DateTime.Now, _currentUser.UserId)));
			}

			await _repository.AddAsync(entity);

			_outbox.Enqueue(Topics.ManagementParents, entity.Id.ToString(),
				ParentEventFactory.From(entity, ParentChangeType.Created, _currentUser.UserId, passwordHash));

			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
