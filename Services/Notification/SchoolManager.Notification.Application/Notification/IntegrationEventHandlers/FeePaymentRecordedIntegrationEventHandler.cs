using System.Globalization;
using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// A fee payment was recorded (Kafka, "financial.fee-payment-recorded"): a Fee notification for that student only
    /// (their parents) with the amount and what is left to pay, pushed to the parents' phones.
    /// A system notification: shown in the parent app, not in the admin notifications page. One per event.
    /// </summary>
    public class FeePaymentRecordedIntegrationEventHandler(
        INotificationRepository repository,
        INotificationPushService push,
        IUnitOfWork unitOfWork,
        ILogger<FeePaymentRecordedIntegrationEventHandler> logger)
        : IIntegrationEventHandler<FeePaymentRecordedIntegrationEvent>
    {
        private static string Money(decimal value, string currency) =>
            $"{value.ToString("#,0.##", CultureInfo.InvariantCulture)} {currency}".Trim();

        public async Task HandleAsync(FeePaymentRecordedIntegrationEvent evt, CancellationToken cancellationToken)
        {
            if (evt.StudentId == Guid.Empty)
                return;
            if (await repository.ExistsForSourceEventAsync(evt.EventId, cancellationToken))
            {
                logger.LogInformation("Fee {FeeId}: payment notification of event {EventId} already created", evt.StudentFeeId, evt.EventId);
                return;
            }

            var now = DateTime.Now;
            var fee = evt.IsBusFee ? "رسوم الباص" : "الرسوم الدراسية";
            var title = $"تم استلام دفعة من {fee}";
            var description =
                $"المبلغ المدفوع: {Money(evt.Amount, evt.Currency)} — بتاريخ {evt.PaidOn:yyyy-MM-dd}" +
                (evt.Balance > 0
                    ? $"\nالمتبقي: {Money(evt.Balance, evt.Currency)}"
                    : "\nتم سداد الرسوم بالكامل ✓");

            var notification = new Domain.Notification(
                title,
                description,
                DateOnly.FromDateTime(now),
                NotificationTypeEnum.Fee,
                DefaultImages.Notification,
                new AuditInfo(now, evt.RecordedBy),
                evt.AcademicYearId is Guid year && year != Guid.Empty ? year : null);

            // Only this student (a row without grade / class): their parents see it and get the push
            notification.AddTarget(null, null, null, evt.StudentId, new AuditInfo(now, evt.RecordedBy));
            notification.MarkSystem(NotificationSource.FeePayment, evt.EventId);

            await repository.AddAsync(notification);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // To the parents' phones (failures are logged by the push service, never thrown)
            await push.PushNotificationAsync(notification, cancellationToken);
            if (notification.PushSentAt is not null)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Fee {FeeId}: payment notification created for student {StudentId}", evt.StudentFeeId, evt.StudentId);
        }
    }
}
