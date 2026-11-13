using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateTimetableCommandHandler : ICommandHandler<CreateTimetableCommand, Guid>
	{
		private readonly ITimetableRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IGradeRepository _gradeRepository;

		private readonly ICurrentUserService _currentUser;

		public CreateTimetableCommandHandler(ICurrentUserService currentUser, ITimetableRepository repository, IGradeRepository gradeRepository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_gradeRepository = gradeRepository;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateTimetableCommand command, CancellationToken cancellationToken)
		{
			var grade = await _gradeRepository.GetByIdAsync(command.GradeId)
				?? throw new InvalidOperationException("Grade not found.");

			if (grade.StageId != command.StageId)
				throw new InvalidOperationException("Grade does not belong to the selected stage.");

			var entity = new Timetable(
				command.StageId,
				command.GradeId,
                command.AcademicYearId,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

			foreach (var slot in command.Slots)
			{
				entity.AddSlot(new TimetableSlot(
					slot.Day,
					new NameLocalized(slot.NameAr, slot.NameEn),
					slot.StartTime,
					slot.EndTime,
					slot.SubjectId,
					slot.TeacherId,
					slot.GradeId,
					slot.SectionId,
					new AuditInfo(DateTime.Now, _currentUser.UserId)));
			}

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
