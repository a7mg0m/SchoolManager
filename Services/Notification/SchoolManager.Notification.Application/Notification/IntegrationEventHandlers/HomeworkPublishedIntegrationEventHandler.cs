using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// A teacher published a homework (Kafka, "examshomework.homework-published"): a Homework notification for the parents
    /// of those classes, with the homework picture (wwwroot/default/homework-default.png), pushed to their phones.
    /// It is a system notification: shown in the parent app, not in the admin notifications page.
    /// Messages may arrive more than once: one notification per event.
    /// </summary>
    public class HomeworkPublishedIntegrationEventHandler(
        INotificationRepository repository,
        INotificationPushService push,
        IUnitOfWork unitOfWork,
        ILogger<HomeworkPublishedIntegrationEventHandler> logger)
        : IIntegrationEventHandler<HomeworkPublishedIntegrationEvent>
    {
        private const int TitleMax = 200;
        private const int DescriptionMax = 2000;

        public async Task HandleAsync(HomeworkPublishedIntegrationEvent evt, CancellationToken cancellationToken)
        {
            if (evt.SectionIds.Count == 0)
                return;
            if (await repository.ExistsForSourceEventAsync(evt.EventId, cancellationToken))
            {
                logger.LogInformation("Homework {HomeworkId}: notification of event {EventId} already created", evt.HomeworkId, evt.EventId);
                return;
            }

            var now = DateTime.Now;
            var title = Shorten($"واجب جديد: {evt.Title}".Trim(), TitleMax);
            var details = string.IsNullOrWhiteSpace(evt.Description) ? string.Empty : evt.Description.Trim() + "\n\n";
            var description = Shorten($"{details}موعد التسليم: {evt.DueDate:yyyy-MM-dd}", DescriptionMax);

            var notification = new Domain.Notification(
                title,
                description,
                DateOnly.FromDateTime(now),
                NotificationTypeEnum.Homework,
                DefaultImages.Homework,
                new AuditInfo(now, evt.PublishedBy),
                evt.AcademicYearId == Guid.Empty ? null : evt.AcademicYearId);

            // Class rows also carry their grade (same as the admin page saves them)
            foreach (var sectionId in evt.SectionIds.Distinct())
                notification.AddTarget(evt.GradeId == Guid.Empty ? null : evt.GradeId, sectionId, null, null, new AuditInfo(now, evt.PublishedBy));

            notification.MarkSystem(NotificationSource.Homework, evt.EventId);

            await repository.AddAsync(notification);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // To the parents' phones (failures are logged by the push service, never thrown)
            await push.PushNotificationAsync(notification, cancellationToken);
            if (notification.PushSentAt is not null)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Homework {HomeworkId}: notification created for {Count} class(es)", evt.HomeworkId, evt.SectionIds.Count);
        }

        private static string Shorten(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";
    }
}
