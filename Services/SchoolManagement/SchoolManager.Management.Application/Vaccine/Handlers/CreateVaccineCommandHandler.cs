using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateVaccineCommandHandler : ICommandHandler<CreateVaccineCommand, Guid>
	{
		private readonly ICurrentUserService _currentUser;
		private readonly IVaccineRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateVaccineCommandHandler(ICurrentUserService currentUser, IVaccineRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateVaccineCommand command, CancellationToken cancellationToken)
		{
			var entity = new Vaccine(
				new NameLocalized(command.NameAr, command.NameEn),
				command.TotalDoses,
				command.IsMandatory,
				new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
