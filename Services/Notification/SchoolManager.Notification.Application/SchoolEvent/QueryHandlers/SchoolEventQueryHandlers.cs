using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
	/// <summary>All events (with targets), newest event date first.</summary>
	public class GetAllSchoolEventsQueryHandler(ISchoolEventRepository repository, IMapper mapper) : IQueryHandler<GetAllSchoolEventsQuery, List<SchoolEventDto>>
	{
		public async Task<List<SchoolEventDto>> Handle(GetAllSchoolEventsQuery request, CancellationToken cancellationToken)
		{
			var items = await repository.GetAllAsync();
			var ordered = items
				.OrderByDescending(x => x.EventDate)
				.ThenByDescending(x => x.StartTime)
				.ToList();
			return mapper.Map<List<SchoolEventDto>>(ordered);
		}
	}

	public class GetSchoolEventByIdQueryHandler(ISchoolEventRepository repository, IMapper mapper) : IQueryHandler<GetSchoolEventByIdQuery, SchoolEventDto?>
	{
		public async Task<SchoolEventDto?> Handle(GetSchoolEventByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await repository.GetByIdAsync(request.Id);
			return entity is null ? null : mapper.Map<SchoolEventDto>(entity);
		}
	}
}
