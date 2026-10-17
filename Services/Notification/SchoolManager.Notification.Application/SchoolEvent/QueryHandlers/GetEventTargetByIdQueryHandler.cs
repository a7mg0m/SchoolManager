using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
	public class GetEventTargetByIdQueryHandler(IEventTargetRepository repository, IMapper mapper) : IQueryHandler<GetEventTargetByIdQuery, EventTargetDto?>
	{
		public async Task<EventTargetDto?> Handle(GetEventTargetByIdQuery request, CancellationToken cancellationToken)
		{
			var item = await repository.GetByIdAsync(request.Id);
			return item is null ? null : mapper.Map<EventTargetDto>(item);
		}
	}
}
