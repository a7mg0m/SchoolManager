using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllSubjectsQueryHandler : ICommandHandler<GetAllSubjectsQuery, List<SubjectDto>>
	{
		private readonly ISubjectRepository _repository;
		private readonly IMapper _mapper;

		public GetAllSubjectsQueryHandler(ISubjectRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<SubjectDto>> Handle(GetAllSubjectsQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<SubjectDto>>(entities);
		}
	}
}
