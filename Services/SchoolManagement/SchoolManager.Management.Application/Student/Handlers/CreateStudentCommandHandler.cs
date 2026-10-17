using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateStudentCommandHandler : ICommandHandler<CreateStudentCommand, Guid>
	{
		private readonly IStudentRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateStudentCommandHandler(IStudentRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateStudentCommand command, CancellationToken cancellationToken)
		{
			var entity = new Student(
				new NameLocalized(command.FullNameAr, command.FullNameEn),
                command.DateOfBirth,
                command.NationalId,
                command.Gender,
                command.PhoneNumber,
                command.Email,
                command.Address,
                command.GradeId,
                command.SectionId,
                command.AcademicYearId,
                command.ParentId,
                new AuditInfo(DateTime.Now, command.CreatedBy));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
