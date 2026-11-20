using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateVaccinationCampaignCommandHandler : ICommandHandler<CreateVaccinationCampaignCommand, Guid>
	{
		private readonly ICurrentUserService _currentUser;
		private readonly IVaccinationCampaignRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateVaccinationCampaignCommandHandler(ICurrentUserService currentUser, IVaccinationCampaignRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateVaccinationCampaignCommand command, CancellationToken cancellationToken)
		{
			var entity = new VaccinationCampaign(
				command.VaccineId,
				command.Title.Trim(),
				command.StartDate,
				command.EndDate,
				new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
