using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllTimetablesQueryHandler : ICommandHandler<GetAllTimetablesQuery, List<TimetableDto>>
	{
		private readonly ITimetableRepository _repository;
		private readonly IMapper _mapper;

		public GetAllTimetablesQueryHandler(ITimetableRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<TimetableDto>> Handle(GetAllTimetablesQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<TimetableDto>>(entities);
		}
	}
}
