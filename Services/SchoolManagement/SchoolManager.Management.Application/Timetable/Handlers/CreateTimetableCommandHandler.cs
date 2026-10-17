using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateTimetableCommandHandler : ICommandHandler<CreateTimetableCommand, Guid>
	{
		private readonly ITimetableRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateTimetableCommandHandler(ITimetableRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateTimetableCommand command, CancellationToken cancellationToken)
		{
			var entity = new Timetable(
				command.GradeId,
                command.AcademicYearId,
                new AuditInfo(DateTime.Now, command.CreatedBy));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
