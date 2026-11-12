using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateSubjectCommandHandler : ICommandHandler<CreateSubjectCommand, Guid>
	{
		private readonly ISubjectRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IGradeRepository _gradeRepository;

		public CreateSubjectCommandHandler(ISubjectRepository repository, IUnitOfWork unitOfWork, IGradeRepository gradeRepository)
		{
			_gradeRepository = gradeRepository;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateSubjectCommand command, CancellationToken cancellationToken)
		{
			var grade = await _gradeRepository.GetByIdAsync(command.GradeId)
				?? throw new InvalidOperationException("Grade not found.");

			var entity = new Subject(
				new NameLocalized(command.NameAr, command.NameEn),
				grade.StageId,
				command.GradeId,
				command.TeacherIds,
                new AuditInfo(DateTime.Now, command.CreatedBy));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
