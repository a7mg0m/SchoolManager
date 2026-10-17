using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateParentCommandHandler : ICommandHandler<CreateParentCommand, Guid>
	{
		private readonly IParentRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateParentCommandHandler(IParentRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateParentCommand command, CancellationToken cancellationToken)
		{
			var entity = new Parent(
				new NameLocalized(command.FullNameAr, command.FullNameEn),
                command.PhoneNumber,
                command.Email,
                command.Address,
                new AuditInfo(DateTime.Now, command.CreatedBy));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
