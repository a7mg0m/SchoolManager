using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateSemesterCommandHandler : ICommandHandler<CreateSemesterCommand, Guid>
	{
		private readonly ISemesterRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateSemesterCommandHandler(ISemesterRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateSemesterCommand command, CancellationToken cancellationToken)
		{
			var entity = new Semester(
				command.Name,
                command.AcademicYearId,
                new AuditInfo(DateTime.Now, command.CreatedBy));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
