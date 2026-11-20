using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetClinicVisitsByStudentIdQueryHandler : ICommandHandler<GetClinicVisitsByStudentIdQuery, List<ClinicVisitDto>>
	{
		private readonly IClinicVisitRepository _repository;
		private readonly IMapper _mapper;

		public GetClinicVisitsByStudentIdQueryHandler(IClinicVisitRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<ClinicVisitDto>> Handle(GetClinicVisitsByStudentIdQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetByStudentIdAsync(request.StudentId);

			return _mapper.Map<List<ClinicVisitDto>>(entities);
		}
	}
}
