using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
    public record GetAllNotificationsQuery : IQuery<List<NotificationDto>>;

    public record GetNotificationByIdQuery(Guid Id) : IQuery<NotificationDto?>;
}
