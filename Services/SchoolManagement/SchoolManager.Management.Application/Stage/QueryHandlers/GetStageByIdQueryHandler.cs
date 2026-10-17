using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetStageByIdQueryHandler : ICommandHandler<GetStageByIdQuery, StageDto?>
	{
		private readonly IStageRepository _repository;
		private readonly IMapper _mapper;

		public GetStageByIdQueryHandler(IStageRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<StageDto?> Handle(GetStageByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			return entity is null ? null : _mapper.Map<StageDto>(entity);
		}
	}
}
