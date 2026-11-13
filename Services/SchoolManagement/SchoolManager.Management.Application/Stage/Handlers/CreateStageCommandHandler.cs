using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateStageCommandHandler : ICommandHandler<CreateStageCommand, Guid>
	{
		private readonly IStageRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		private readonly ICurrentUserService _currentUser;

		public CreateStageCommandHandler(ICurrentUserService currentUser, IStageRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateStageCommand command, CancellationToken cancellationToken)
		{
			var entity = new Stage(
				new NameLocalized(command.NameAr, command.NameEn),
                new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}

