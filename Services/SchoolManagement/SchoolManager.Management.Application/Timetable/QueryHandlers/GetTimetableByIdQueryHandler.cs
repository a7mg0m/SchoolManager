using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetTimetableByIdQueryHandler : ICommandHandler<GetTimetableByIdQuery, TimetableDto?>
	{
		private readonly ITimetableRepository _repository;
		private readonly IMapper _mapper;

		public GetTimetableByIdQueryHandler(ITimetableRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<TimetableDto?> Handle(GetTimetableByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			return entity is null ? null : _mapper.Map<TimetableDto>(entity);
		}
	}
}
