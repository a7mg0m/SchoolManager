using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetActiveVaccinesQueryHandler : ICommandHandler<GetActiveVaccinesQuery, List<VaccineDto>>
	{
		private readonly IVaccineRepository _repository;
		private readonly IMapper _mapper;

		public GetActiveVaccinesQueryHandler(IVaccineRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<VaccineDto>> Handle(GetActiveVaccinesQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<VaccineDto>>(entities.Where(x => x.IsActive).ToList());
		}
	}
}
