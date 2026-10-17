using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllStagesQueryHandler : ICommandHandler<GetAllStagesQuery, List<StageDto>>
	{
		private readonly IStageRepository _repository;
		private readonly IMapper _mapper;

		public GetAllStagesQueryHandler(IStageRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<StageDto>> Handle(GetAllStagesQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<StageDto>>(entities);
		}
	}
}
