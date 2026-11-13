using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateSectionCommandHandler : ICommandHandler<CreateSectionCommand, Guid>
	{
		private readonly ISectionRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		private readonly ICurrentUserService _currentUser;

		public CreateSectionCommandHandler(ICurrentUserService currentUser, ISectionRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateSectionCommand command, CancellationToken cancellationToken)
		{
			var entity = new Section(
				new NameLocalized(command.NameAr, command.NameEn),
                command.GradeId,
                command.StageId,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
