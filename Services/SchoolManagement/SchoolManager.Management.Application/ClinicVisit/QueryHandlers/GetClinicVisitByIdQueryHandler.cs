using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetClinicVisitByIdQueryHandler : ICommandHandler<GetClinicVisitByIdQuery, ClinicVisitDto?>
	{
		private readonly IClinicVisitRepository _repository;
		private readonly IMapper _mapper;

		public GetClinicVisitByIdQueryHandler(IClinicVisitRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<ClinicVisitDto?> Handle(GetClinicVisitByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdWithMedicationsAsync(request.Id);

			return entity is null ? null : _mapper.Map<ClinicVisitDto>(entity);
		}
	}
}
