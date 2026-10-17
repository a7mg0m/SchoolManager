using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
	public record GetAllEventTargetsQuery() : IQuery<List<EventTargetDto>>;
}
