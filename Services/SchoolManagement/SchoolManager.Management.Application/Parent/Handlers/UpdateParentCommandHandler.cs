using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	/// <summary>
	/// Updates the parent and its students and queues a ParentIntegrationEvent (Updated) in the same transaction;
	/// the Identity service then updates (or creates) the parent's User and UserProfile.
	/// </summary>
	public class UpdateParentCommandHandler : ICommandHandler<UpdateParentCommand, bool>
	{
		private readonly IParentRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IGradeRepository _gradeRepository;
		private readonly IStudentRepository _studentRepository;
		private readonly IIntegrationEventOutbox _outbox;

		private readonly ICurrentUserService _currentUser;

		public UpdateParentCommandHandler(ICurrentUserService currentUser, IParentRepository repository, IStudentRepository studentRepository, IGradeRepository gradeRepository, IUnitOfWork unitOfWork, IIntegrationEventOutbox outbox)
		{
			_currentUser = currentUser;
			_gradeRepository = gradeRepository;
			_studentRepository = studentRepository;
			_repository = repository;
			_unitOfWork = unitOfWork;
			_outbox = outbox;
		}

		public async Task<bool> Handle(UpdateParentCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdWithStudentsAsync(request.Id);

			if (entity is null)
				return false;

			// Login account (kept in sync by the Identity service): an empty password keeps the current one
			var loginName = await ParentAccount.ValidateLoginNameAsync(request.LoginName, entity.Id, _repository);
			var passwordHash = ParentAccount.HashPassword(request.Password, required: false);
			entity.SetLoginName(loginName);

			entity.Update(
				new NameLocalized(request.FullNameAr, request.FullNameEn),
                request.PhoneNumber,
                request.Email,
                request.Address,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            AuditInfo NewAudit() => new AuditInfo(DateTime.Now, _currentUser.UserId);
            var incomingIds = request.Students.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();

            // New students (no Id) must not reuse a national ID that already belongs to someone
            var existingIds = await _studentRepository.GetExistingNationalIdsAsync(
                request.Students.Where(s => !s.Id.HasValue).Select(s => s.NationalId), cancellationToken);
            if (existingIds.Count > 0)
                throw new DuplicateNationalIdException(existingIds);

            foreach (var removed in entity.Students.Where(s => !incomingIds.Contains(s.Id)).ToList())
            {
                entity.Students.Remove(removed);
                _studentRepository.Remove(removed);
            }

            foreach (var s in request.Students)
            {
                var grade = await _gradeRepository.GetByIdAsync(s.GradeId)
                    ?? throw new InvalidOperationException("Grade not found.");
                var existing = s.Id.HasValue ? entity.Students.FirstOrDefault(x => x.Id == s.Id.Value) : null;

                if (existing is not null)
                {
                    existing.Update(
                        new NameLocalized(s.FullNameAr, s.FullNameEn),
                        s.PhoneNumber,
                        s.Email,
                        s.Address,
                        grade.StageId,
                        s.GradeId,
                        s.SectionId,
                        s.AcademicYearId,
                        NewAudit());
                }
                else
                {
                    await _studentRepository.AddAsync(new Student(
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
                        NewAudit()));
                }
            }

			_outbox.Enqueue(Topics.ManagementParents, entity.Id.ToString(),
				ParentEventFactory.From(entity, ParentChangeType.Updated, _currentUser.UserId, passwordHash));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
