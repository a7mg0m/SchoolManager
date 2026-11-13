using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateGradeCommandHandler : ICommandHandler<CreateGradeCommand, Guid>
	{
		private readonly IGradeRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		private readonly ICurrentUserService _currentUser;

		public CreateGradeCommandHandler(ICurrentUserService currentUser, IGradeRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateGradeCommand command, CancellationToken cancellationToken)
		{
			var entity = new Grade(
				new NameLocalized(command.NameAr, command.NameEn),
                command.StageId,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
