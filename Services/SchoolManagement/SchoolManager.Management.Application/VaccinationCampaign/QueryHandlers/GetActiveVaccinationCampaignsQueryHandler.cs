using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetActiveVaccinationCampaignsQueryHandler : ICommandHandler<GetActiveVaccinationCampaignsQuery, List<VaccinationCampaignDto>>
	{
		private readonly IVaccinationCampaignRepository _repository;
		private readonly IMapper _mapper;

		public GetActiveVaccinationCampaignsQueryHandler(IVaccinationCampaignRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<VaccinationCampaignDto>> Handle(GetActiveVaccinationCampaignsQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<VaccinationCampaignDto>>(entities.Where(x => x.IsActive).ToList());
		}
	}
}
