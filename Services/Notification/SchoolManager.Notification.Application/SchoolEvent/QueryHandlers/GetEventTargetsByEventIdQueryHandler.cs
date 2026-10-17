using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
	public class GetEventTargetsByEventIdQueryHandler(IEventTargetRepository repository, IMapper mapper) : IQueryHandler<GetEventTargetsByEventIdQuery, List<EventTargetDto>>
	{
		public async Task<List<EventTargetDto>> Handle(GetEventTargetsByEventIdQuery request, CancellationToken cancellationToken)
		{
			var items = await repository.SearchAsync(x => x.EventId == request.EventId);
			return mapper.Map<List<EventTargetDto>>(items);
		}
	}
}
