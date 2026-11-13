using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateSemesterCommandHandler : ICommandHandler<CreateSemesterCommand, Guid>
	{
		private readonly ISemesterRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		private readonly ICurrentUserService _currentUser;

		public CreateSemesterCommandHandler(ICurrentUserService currentUser, ISemesterRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateSemesterCommand command, CancellationToken cancellationToken)
		{
			var entity = new Semester(
				command.Name,
                command.AcademicYearId,
                command.StartDate,
                command.EndDate,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
