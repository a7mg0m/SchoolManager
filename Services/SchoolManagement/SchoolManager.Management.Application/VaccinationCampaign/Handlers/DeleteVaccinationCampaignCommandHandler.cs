using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class DeleteVaccinationCampaignCommandHandler : ICommandHandler<DeleteVaccinationCampaignCommand, bool>
	{
		private readonly IVaccinationCampaignRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public DeleteVaccinationCampaignCommandHandler(IVaccinationCampaignRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(DeleteVaccinationCampaignCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			_repository.Remove(entity);

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
