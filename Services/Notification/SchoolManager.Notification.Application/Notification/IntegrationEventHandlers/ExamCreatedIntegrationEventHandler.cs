using System.Globalization;
using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// A new exam was saved (Kafka, "examshomework.exam-created"): an Exam notification for the parents of its class
    /// (or of the whole grade), with the exam picture (wwwroot/default/exam-default.png), pushed to their phones.
    /// A system notification: shown in the parent app, not in the admin notifications page. One per event.
    /// </summary>
    public class ExamCreatedIntegrationEventHandler(
        INotificationRepository repository,
        INotificationPushService push,
        IUnitOfWork unitOfWork,
        ILogger<ExamCreatedIntegrationEventHandler> logger)
        : IIntegrationEventHandler<ExamCreatedIntegrationEvent>
    {
        private static string TypeName(int type) => type switch
        {
            1 => "اختبار قصير",
            2 => "امتحان شهري",
            3 => "امتحان منتصف الفصل",
            4 => "امتحان نهائي",
            _ => "امتحان"
        };

        private static string Mark(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        public async Task HandleAsync(ExamCreatedIntegrationEvent evt, CancellationToken cancellationToken)
        {
            if (evt.GradeId == Guid.Empty)
                return;
            if (await repository.ExistsForSourceEventAsync(evt.EventId, cancellationToken))
            {
                logger.LogInformation("Exam {ExamId}: notification of event {EventId} already created", evt.ExamId, evt.EventId);
                return;
            }

            var now = DateTime.Now;
            var title = $"{TypeName(evt.ExamType)} جديد";
            var description =
                $"موعد الامتحان: {evt.ExamDate:yyyy-MM-dd} من {evt.StartTime:HH\\:mm} إلى {evt.EndTime:HH\\:mm}" +
                $"\nالدرجة الكاملة: {Mark(evt.Weight)} — درجة النجاح: {Mark(evt.SuccessScore)}";

            var notification = new Domain.Notification(
                title,
                description,
                DateOnly.FromDateTime(now),
                NotificationTypeEnum.Exam,
                DefaultImages.Exam,
                new AuditInfo(now, evt.CreatedBy),
                evt.AcademicYearId == Guid.Empty ? null : evt.AcademicYearId);

            // The exam's class (the row carries its grade), or the whole grade
            notification.AddTarget(evt.GradeId, evt.SectionId, null, null, new AuditInfo(now, evt.CreatedBy));
            notification.MarkSystem(NotificationSource.Exam, evt.EventId);

            await repository.AddAsync(notification);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // To the parents' phones (failures are logged by the push service, never thrown)
            await push.PushNotificationAsync(notification, cancellationToken);
            if (notification.PushSentAt is not null)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Exam {ExamId}: notification created for {Audience}", evt.ExamId,
                evt.SectionId is null ? "the whole grade" : "one class");
        }
    }
}
