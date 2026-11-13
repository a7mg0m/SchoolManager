using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateStudentCommandHandler : ICommandHandler<CreateStudentCommand, Guid>
	{
		private readonly IStudentRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IGradeRepository _gradeRepository;

		private readonly ICurrentUserService _currentUser;

		public CreateStudentCommandHandler(ICurrentUserService currentUser, IStudentRepository repository, IGradeRepository gradeRepository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_gradeRepository = gradeRepository;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateStudentCommand command, CancellationToken cancellationToken)
		{
			var existingIds = await _repository.GetExistingNationalIdsAsync(new[] { command.NationalId }, cancellationToken);
			if (existingIds.Count > 0)
				throw new DuplicateNationalIdException(existingIds);

			var grade = await _gradeRepository.GetByIdAsync(command.GradeId)
				?? throw new InvalidOperationException("Grade not found.");

			var entity = new Student(
				new NameLocalized(command.FullNameAr, command.FullNameEn),
                command.DateOfBirth,
                command.NationalId,
                command.Gender,
                command.PhoneNumber,
                command.Email,
                command.Address,
                grade.StageId,
                command.GradeId,
                command.SectionId,
                command.AcademicYearId,
                command.ParentId,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
