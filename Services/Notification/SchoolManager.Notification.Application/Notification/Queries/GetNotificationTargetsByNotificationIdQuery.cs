using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
	public record GetNotificationTargetsByNotificationIdQuery(Guid NotificationId) : IQuery<List<NotificationTargetDto>>;
}
