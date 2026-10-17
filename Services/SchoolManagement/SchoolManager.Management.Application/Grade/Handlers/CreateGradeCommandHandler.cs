using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateGradeCommandHandler : ICommandHandler<CreateGradeCommand, Guid>
	{
		private readonly IGradeRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateGradeCommandHandler(IGradeRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateGradeCommand command, CancellationToken cancellationToken)
		{
			var entity = new Grade(
				new NameLocalized(command.NameAr, command.NameEn),
                command.StageId,
                new AuditInfo(DateTime.Now, command.CreatedBy));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
