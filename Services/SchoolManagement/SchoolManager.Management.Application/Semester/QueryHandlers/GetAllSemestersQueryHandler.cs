using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllSemestersQueryHandler : ICommandHandler<GetAllSemestersQuery, List<SemesterDto>>
	{
		private readonly ISemesterRepository _repository;
		private readonly IMapper _mapper;

		public GetAllSemestersQueryHandler(ISemesterRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<SemesterDto>> Handle(GetAllSemestersQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<SemesterDto>>(entities);
		}
	}
}
