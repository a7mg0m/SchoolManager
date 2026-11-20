using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllVaccinationCampaignsQueryHandler : ICommandHandler<GetAllVaccinationCampaignsQuery, List<VaccinationCampaignDto>>
	{
		private readonly IVaccinationCampaignRepository _repository;
		private readonly IMapper _mapper;

		public GetAllVaccinationCampaignsQueryHandler(IVaccinationCampaignRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<VaccinationCampaignDto>> Handle(GetAllVaccinationCampaignsQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<VaccinationCampaignDto>>(entities);
		}
	}
}
