using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolManager.Notification.Application;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    /// <summary>
    /// Pushes notifications whose SentOn date has come (created for a later day) and marks them PushSentAt.
    /// Runs shortly after start and then every <see cref="Interval"/>.
    /// </summary>
    public class DuePushNotificationsService(IServiceScopeFactory scopes, ILogger<DuePushNotificationsService> logger)
        : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Let the service finish starting first
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            using var timer = new PeriodicTimer(Interval);
            do
            {
                try
                {
                    await PushDueAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Pushing due notifications failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task PushDueAsync(CancellationToken cancellationToken)
        {
            using var scope = scopes.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            var push = scope.ServiceProvider.GetRequiredService<INotificationPushService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<BuildingBlocks.IUnitOfWork>();

            var due = await repository.GetDueForPushAsync(DateOnly.FromDateTime(DateTime.Now), cancellationToken);
            if (due.Count == 0) return;

            foreach (var notification in due)
                await push.PushNotificationAsync(notification, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Pushed {Count} due notification(s)", due.Count(n => n.PushSentAt is not null));
        }
    }
}
