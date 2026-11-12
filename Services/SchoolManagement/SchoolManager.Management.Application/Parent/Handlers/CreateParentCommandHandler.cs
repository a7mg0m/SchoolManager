using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateParentCommandHandler : ICommandHandler<CreateParentCommand, Guid>
	{
		private readonly IParentRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IGradeRepository _gradeRepository;

		public CreateParentCommandHandler(IParentRepository repository, IGradeRepository gradeRepository, IUnitOfWork unitOfWork)
		{
			_gradeRepository = gradeRepository;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateParentCommand command, CancellationToken cancellationToken)
		{
			var entity = new Parent(
				new NameLocalized(command.FullNameAr, command.FullNameEn),
                command.PhoneNumber,
                command.Email,
                command.Address,
				new AuditInfo(DateTime.Now, command.CreatedBy));

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
					new AuditInfo(DateTime.Now, command.CreatedBy)));
			}

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
