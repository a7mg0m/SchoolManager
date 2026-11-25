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
		private readonly ISemesterRepository _semesterRepository;

		private readonly ICurrentUserService _currentUser;

		public CreateTimetableCommandHandler(ICurrentUserService currentUser, ITimetableRepository repository, IGradeRepository gradeRepository, ISemesterRepository semesterRepository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_gradeRepository = gradeRepository;
			_semesterRepository = semesterRepository;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateTimetableCommand command, CancellationToken cancellationToken)
		{
			var grade = await _gradeRepository.GetByIdAsync(command.GradeId)
				?? throw new InvalidOperationException("Grade not found.");

			if (grade.StageId != command.StageId)
				throw new InvalidOperationException("Grade does not belong to the selected stage.");

			var semester = await _semesterRepository.GetByIdAsync(command.SemesterId)
				?? throw new InvalidOperationException("Semester not found.");
			if (semester.AcademicYearId != command.AcademicYearId)
				throw new InvalidOperationException("Semester does not belong to the selected academic year.");

			var entity = new Timetable(
				command.StageId,
				command.GradeId,
                command.AcademicYearId,
                command.SemesterId,
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

			entity.SetSchedule(command.DayStartTime, TimetableWorkDayDtoValidator.ToDomain(command.WorkDays ?? new()));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
