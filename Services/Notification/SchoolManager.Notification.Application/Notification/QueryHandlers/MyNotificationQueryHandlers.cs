using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class GetChildNotificationsQueryHandler(INotificationRepository repository)
        : IQueryHandler<GetChildNotificationsQuery, List<MyNotificationDto>>
    {
        public async Task<List<MyNotificationDto>> Handle(GetChildNotificationsQuery query, CancellationToken cancellationToken)
        {
            var child = query.Child;
            var items = await repository.GetForStudentAsync(query.AcademicYearId, child.GradeId, child.SectionId, child.Id,
                DateOnly.FromDateTime(DateTime.Today), cancellationToken);
            return MyNotifications.ToDtos(items);
        }
    }

    public class GetTeacherNotificationsQueryHandler(INotificationRepository repository)
        : IQueryHandler<GetTeacherNotificationsQuery, List<MyNotificationDto>>
    {
        public async Task<List<MyNotificationDto>> Handle(GetTeacherNotificationsQuery query, CancellationToken cancellationToken)
        {
            var items = await repository.GetForTeacherAsync(query.AcademicYearId, query.TeacherId,
                DateOnly.FromDateTime(DateTime.Today), cancellationToken);
            return MyNotifications.ToDtos(items);
        }
    }

    internal static class MyNotifications
    {
        /// <summary>Newest first</summary>
        public static List<MyNotificationDto> ToDtos(IEnumerable<Domain.Notification> items) => items
            .OrderByDescending(n => n.SentOn).ThenByDescending(n => n.AuditInfo.CreatedOn)
            .Select(n => new MyNotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Description = n.Description,
                SentOn = n.SentOn,
                NotificationType = n.NotificationType,
                ImageUrl = n.ImageUrl,
                ForEveryone = n.Targets.Count == 0
            })
            .ToList();
    }
}
