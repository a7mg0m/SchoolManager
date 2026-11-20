using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllClinicVisitsQueryHandler : ICommandHandler<GetAllClinicVisitsQuery, List<ClinicVisitDto>>
	{
		private readonly IClinicVisitRepository _repository;
		private readonly IMapper _mapper;

		public GetAllClinicVisitsQueryHandler(IClinicVisitRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<ClinicVisitDto>> Handle(GetAllClinicVisitsQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<ClinicVisitDto>>(entities);
		}
	}
}
