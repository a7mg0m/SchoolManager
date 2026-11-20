using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
    /// <param name="AcademicYearId">Only the notifications of this academic year (null: all)</param>
    public record GetAllNotificationsQuery(Guid? AcademicYearId = null) : IQuery<List<NotificationDto>>;

    public record GetNotificationByIdQuery(Guid Id) : IQuery<NotificationDto?>;
}
