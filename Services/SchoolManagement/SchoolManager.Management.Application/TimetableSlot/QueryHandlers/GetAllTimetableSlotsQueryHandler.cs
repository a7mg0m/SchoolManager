using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllTimetableSlotsQueryHandler : ICommandHandler<GetAllTimetableSlotsQuery, List<TimetableSlotDto>>
	{
		private readonly ITimetableSlotRepository _repository;
		private readonly IMapper _mapper;

		public GetAllTimetableSlotsQueryHandler(ITimetableSlotRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<TimetableSlotDto>> Handle(GetAllTimetableSlotsQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<TimetableSlotDto>>(entities);
		}
	}
}
