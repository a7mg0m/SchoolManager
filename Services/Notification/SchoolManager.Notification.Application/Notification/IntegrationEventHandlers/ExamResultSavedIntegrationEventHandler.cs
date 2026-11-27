using System.Globalization;
using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// A student's exam score was saved (Kafka, "examshomework.exam-result-saved"): an Exam notification for that
    /// student only (their parents), with the score and whether it passed, pushed to the parents' phones.
    /// A system notification: shown in the parent app, not in the admin notifications page. One per event.
    /// </summary>
    public class ExamResultSavedIntegrationEventHandler(
        INotificationRepository repository,
        INotificationPushService push,
        IUnitOfWork unitOfWork,
        ILogger<ExamResultSavedIntegrationEventHandler> logger)
        : IIntegrationEventHandler<ExamResultSavedIntegrationEvent>
    {
        private static string TypeName(int type) => type switch
        {
            1 => "الاختبار القصير",
            2 => "الامتحان الشهري",
            3 => "امتحان منتصف الفصل",
            4 => "الامتحان النهائي",
            5 => "امتحان الدور الثاني",
            _ => "الامتحان"
        };

        private static string Mark(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        public async Task HandleAsync(ExamResultSavedIntegrationEvent evt, CancellationToken cancellationToken)
        {
            if (evt.StudentId == Guid.Empty)
                return;
            if (await repository.ExistsForSourceEventAsync(evt.EventId, cancellationToken))
            {
                logger.LogInformation("Exam {ExamId}: result notification of event {EventId} already created", evt.ExamId, evt.EventId);
                return;
            }

            var now = DateTime.Now;
            var passed = evt.Score >= evt.SuccessScore;
            var title = evt.IsCorrection
                ? $"تعديل نتيجة {TypeName(evt.ExamType)}"
                : $"نتيجة {TypeName(evt.ExamType)}";
            var description =
                $"الدرجة: {Mark(evt.Score)} من {Mark(evt.Weight)} — {(passed ? "ناجح ✓" : "لم ينجح")}" +
                $"\nدرجة النجاح: {Mark(evt.SuccessScore)} — تاريخ الامتحان: {evt.ExamDate:yyyy-MM-dd}";

            var notification = new Domain.Notification(
                title,
                description,
                DateOnly.FromDateTime(now),
                NotificationTypeEnum.ExamResult,
                DefaultImages.Exam,
                new AuditInfo(now, evt.SavedBy),
                evt.AcademicYearId == Guid.Empty ? null : evt.AcademicYearId);

            // Only this student (a row without grade / class): their parents see it and get the push
            notification.AddTarget(null, null, null, evt.StudentId, new AuditInfo(now, evt.SavedBy));
            notification.MarkSystem(NotificationSource.ExamResult, evt.EventId);

            await repository.AddAsync(notification);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // To the parents' phones (failures are logged by the push service, never thrown)
            await push.PushNotificationAsync(notification, cancellationToken);
            if (notification.PushSentAt is not null)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Exam {ExamId}: result notification created for student {StudentId}", evt.ExamId, evt.StudentId);
        }
    }
}
