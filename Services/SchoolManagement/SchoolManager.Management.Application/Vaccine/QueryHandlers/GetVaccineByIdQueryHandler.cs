using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetVaccineByIdQueryHandler : ICommandHandler<GetVaccineByIdQuery, VaccineDto?>
	{
		private readonly IVaccineRepository _repository;
		private readonly IMapper _mapper;

		public GetVaccineByIdQueryHandler(IVaccineRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<VaccineDto?> Handle(GetVaccineByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			return entity is null ? null : _mapper.Map<VaccineDto>(entity);
		}
	}
}
