using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetTimetableSlotByIdQueryHandler : ICommandHandler<GetTimetableSlotByIdQuery, TimetableSlotDto?>
	{
		private readonly ITimetableSlotRepository _repository;
		private readonly IMapper _mapper;

		public GetTimetableSlotByIdQueryHandler(ITimetableSlotRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<TimetableSlotDto?> Handle(GetTimetableSlotByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			return entity is null ? null : _mapper.Map<TimetableSlotDto>(entity);
		}
	}
}
