using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateSubjectCommandHandler : ICommandHandler<CreateSubjectCommand, Guid>
	{
		private readonly ISubjectRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateSubjectCommandHandler(ISubjectRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateSubjectCommand command, CancellationToken cancellationToken)
		{

			var entity = new Subject(
				new NameLocalized(command.NameAr, command.NameEn),
				command.GradeId,
				command.TeacherIds,
                new AuditInfo(DateTime.Now, command.CreatedBy));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
