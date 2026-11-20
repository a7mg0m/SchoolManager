using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllVaccinesQueryHandler : ICommandHandler<GetAllVaccinesQuery, List<VaccineDto>>
	{
		private readonly IVaccineRepository _repository;
		private readonly IMapper _mapper;

		public GetAllVaccinesQueryHandler(IVaccineRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<VaccineDto>> Handle(GetAllVaccinesQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<VaccineDto>>(entities);
		}
	}
}
