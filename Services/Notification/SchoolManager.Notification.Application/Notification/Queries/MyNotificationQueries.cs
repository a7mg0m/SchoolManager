using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
    /// <summary>Notifications of a parent's child (the controller checked the child is the parent's).</summary>
    /// <param name="AcademicYearId">Year chosen in the app; null = every year</param>
    public record GetChildNotificationsQuery(ParentChild Child, Guid? AcademicYearId = null) : IQuery<List<MyNotificationDto>>;

    /// <summary>Notifications for a teacher (the controller passes the teacher id from the login token).</summary>
    public record GetTeacherNotificationsQuery(Guid TeacherId, Guid? AcademicYearId = null) : IQuery<List<MyNotificationDto>>;
}
