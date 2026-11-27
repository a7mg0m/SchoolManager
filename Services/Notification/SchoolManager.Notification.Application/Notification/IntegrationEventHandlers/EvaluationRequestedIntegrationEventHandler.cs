using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// A parent asked a teacher for an evaluation (Kafka, "attendance.evaluation-requested"): an EvaluationRequest
    /// notification for that teacher only, with the child's name and the parent's note, pushed to the teacher's phone.
    /// A system notification: shown in the teacher app, not in the admin notifications page. One per event.
    /// </summary>
    public class EvaluationRequestedIntegrationEventHandler(
        INotificationRepository repository,
        INotificationPushService push,
        IUnitOfWork unitOfWork,
        ILogger<EvaluationRequestedIntegrationEventHandler> logger)
        : IIntegrationEventHandler<EvaluationRequestedIntegrationEvent>
    {
        public async Task HandleAsync(EvaluationRequestedIntegrationEvent evt, CancellationToken cancellationToken)
        {
            if (evt.TeacherId == Guid.Empty)
                return;
            if (await repository.ExistsForSourceEventAsync(evt.EventId, cancellationToken))
            {
                logger.LogInformation("Evaluation request {RequestId}: notification of event {EventId} already created", evt.RequestId, evt.EventId);
                return;
            }

            var now = DateTime.Now;
            var student = !string.IsNullOrWhiteSpace(evt.StudentNameAr) ? evt.StudentNameAr : evt.StudentNameEn;
            var title = "طلب تقييم من ولي أمر";
            var description = string.IsNullOrWhiteSpace(student)
                ? "طلب ولي أمر تقييم مستوى ابنه."
                : $"طلب ولي أمر الطالب {student} تقييم مستواه.";
            if (!string.IsNullOrWhiteSpace(evt.Note))
                description += $"\nملاحظة ولي الأمر: {evt.Note.Trim()}";

            var notification = new Domain.Notification(
                title,
                description,
                DateOnly.FromDateTime(now),
                NotificationTypeEnum.EvaluationRequest,
                DefaultImages.Notification,
                new AuditInfo(now, evt.RequestedBy),
                evt.AcademicYearId is Guid year && year != Guid.Empty ? year : null);

            // Only this teacher
            notification.AddTarget(null, null, evt.TeacherId, null, new AuditInfo(now, evt.RequestedBy));
            notification.MarkSystem(NotificationSource.EvaluationRequest, evt.EventId);

            await repository.AddAsync(notification);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // To the teacher's phone (failures are logged by the push service, never thrown)
            await push.PushNotificationAsync(notification, cancellationToken);
            if (notification.PushSentAt is not null)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Evaluation request {RequestId}: notification created for teacher {TeacherId}", evt.RequestId, evt.TeacherId);
        }
    }
}
