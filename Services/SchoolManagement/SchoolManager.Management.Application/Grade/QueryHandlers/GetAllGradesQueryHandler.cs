using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllGradesQueryHandler : ICommandHandler<GetAllGradesQuery, List<GradeDto>>
	{
		private readonly IGradeRepository _repository;
		private readonly IMapper _mapper;

		public GetAllGradesQueryHandler(IGradeRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<GradeDto>> Handle(GetAllGradesQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<GradeDto>>(entities);
		}
	}
}
