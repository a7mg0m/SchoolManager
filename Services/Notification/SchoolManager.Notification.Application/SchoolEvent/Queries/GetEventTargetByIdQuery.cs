using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
	public record GetEventTargetByIdQuery(Guid Id) : IQuery<EventTargetDto?>;
}
