using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
	public record GetEventTargetsByEventIdQuery(Guid EventId) : IQuery<List<EventTargetDto>>;
}
