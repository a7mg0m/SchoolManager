using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
	public record GetAllNotificationTargetsQuery() : IQuery<List<NotificationTargetDto>>;
}
