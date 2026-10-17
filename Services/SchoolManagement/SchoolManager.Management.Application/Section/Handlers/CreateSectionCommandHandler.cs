using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateSectionCommandHandler : ICommandHandler<CreateSectionCommand, Guid>
	{
		private readonly ISectionRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateSectionCommandHandler(ISectionRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateSectionCommand command, CancellationToken cancellationToken)
		{
			var entity = new Section(
				new NameLocalized(command.NameAr, command.NameEn),
                command.GradeId,
                new AuditInfo(DateTime.Now, command.CreatedBy));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
