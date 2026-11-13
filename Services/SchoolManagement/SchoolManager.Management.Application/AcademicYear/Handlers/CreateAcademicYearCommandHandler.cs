using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateAcademicYearCommandHandler : ICommandHandler<CreateAcademicYearCommand, Guid>
	{
		private readonly IAcademicYearRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		private readonly ICurrentUserService _currentUser;

		public CreateAcademicYearCommandHandler(ICurrentUserService currentUser, IAcademicYearRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateAcademicYearCommand command, CancellationToken cancellationToken)
		{
			var entity = new AcademicYear(
				command.Name,
                command.StartDate,
                command.EndDate,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

			foreach (var semester in command.Semesters ?? new())
				entity.Semesters.Add(new Semester(semester.Name, entity.Id, semester.StartDate, semester.EndDate, new AuditInfo(DateTime.Now, _currentUser.UserId)));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
