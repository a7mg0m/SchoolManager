using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateAcademicYearCommandHandler : ICommandHandler<CreateAcademicYearCommand, Guid>
	{
		private readonly IAcademicYearRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateAcademicYearCommandHandler(IAcademicYearRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateAcademicYearCommand command, CancellationToken cancellationToken)
		{
			var entity = new AcademicYear(
				command.Name,
                command.StartDate,
                command.EndDate,
                new AuditInfo(DateTime.Now, command.CreatedBy));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
