using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
	public record GetNotificationTargetByIdQuery(Guid Id) : IQuery<NotificationTargetDto?>;
}
