using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// A bus trip started (Kafka, "attendance.trip-started"): a Bus notification for the parents of the trip's students,
    /// with the bus picture (wwwroot/default/bus-moving.png), pushed to their phones.
    /// A system notification: shown in the parent app, not in the admin notifications page. One per event.
    /// </summary>
    public class TripStartedIntegrationEventHandler(
        INotificationRepository repository,
        INotificationPushService push,
        IUnitOfWork unitOfWork,
        ILogger<TripStartedIntegrationEventHandler> logger)
        : IIntegrationEventHandler<TripStartedIntegrationEvent>
    {
        public async Task HandleAsync(TripStartedIntegrationEvent evt, CancellationToken cancellationToken)
        {
            var students = evt.StudentIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (students.Count == 0)
                return;
            if (await repository.ExistsForSourceEventAsync(evt.EventId, cancellationToken))
            {
                logger.LogInformation("Trip {TripId}: notification of event {EventId} already created", evt.TripId, evt.EventId);
                return;
            }

            var now = DateTime.Now;
            var toHome = evt.TripType == 1;
            var title = toHome ? "الحافلة في طريقها إلى المنزل" : "الحافلة في طريقها إلى المدرسة";
            var startedAt = DateTime.SpecifyKind(evt.StartedAt, DateTimeKind.Utc).ToLocalTime();
            var bus = string.IsNullOrWhiteSpace(evt.PlateNumber) ? evt.BusNumber : $"{evt.BusNumber} ({evt.PlateNumber})";
            var driver = string.IsNullOrWhiteSpace(evt.DriverName) ? string.Empty : $"\nالسائق: {evt.DriverName}";
            var description =
                $"انطلقت الحافلة رقم {bus} الساعة {startedAt:HH:mm}" +
                (toHome ? " في رحلة العودة إلى المنزل." : " في رحلة الذهاب إلى المدرسة.") +
                driver +
                "\nيمكنك متابعة موقع الحافلة من شاشة الحافلة في التطبيق.";

            var notification = new Domain.Notification(
                title,
                description,
                DateOnly.FromDateTime(now),
                NotificationTypeEnum.Bus,
                DefaultImages.BusMoving,
                new AuditInfo(now, evt.StartedBy),
                // The parent app lists notifications of the academic year chosen in the app
                evt.AcademicYearId is Guid year && year != Guid.Empty ? year : null);

            // The trip's students: each one's parents get it
            foreach (var studentId in students)
                notification.AddTarget(null, null, null, studentId, new AuditInfo(now, evt.StartedBy));

            notification.MarkSystem(NotificationSource.Trip, evt.EventId);

            await repository.AddAsync(notification);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // To the parents' phones (failures are logged by the push service, never thrown)
            await push.PushNotificationAsync(notification, cancellationToken);
            if (notification.PushSentAt is not null)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Trip {TripId}: notification created for {Count} student(s)", evt.TripId, students.Count);
        }
    }
}
