using BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SchoolManager.Management.Infrastructure
{
    /// <summary>
    /// Every few seconds, sends pending outbox messages to Kafka (oldest first) and marks them as processed.
    /// When Kafka is unreachable the batch stops at the failed message, so the order is kept,
    /// and it is retried on the next run.
    /// </summary>
    public class OutboxPublisherService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
        private const int BatchSize = 50;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEventPublisher _publisher;
        private readonly ILogger<OutboxPublisherService> _logger;

        public OutboxPublisherService(IServiceScopeFactory scopeFactory, IEventPublisher publisher, ILogger<OutboxPublisherService> logger)
        {
            _scopeFactory = scopeFactory;
            _publisher = publisher;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);
            do
            {
                try
                {
                    await PublishPendingAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // e.g. database not reachable: try again on the next tick
                    _logger.LogError(ex, "Outbox publishing failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task PublishPendingAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ManagementDbContext>();

            var pending = await context.OutboxMessages
                .Where(m => m.ProcessedOn == null)
                .OrderBy(m => m.OccurredOn)
                .Take(BatchSize)
                .ToListAsync(stoppingToken);

            if (pending.Count == 0)
                return;

            foreach (var message in pending)
            {
                try
                {
                    await _publisher.PublishAsync(message.Topic, message.Key, message.Payload, message.EventType, stoppingToken);
                    message.ProcessedOn = DateTime.UtcNow;
                    message.LastError = null;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    message.Attempts++;
                    message.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                    _logger.LogWarning("Publishing outbox message {Id} ({EventType}) failed (attempt {Attempts}): {Error}",
                        message.Id, message.EventType, message.Attempts, ex.Message);
                    break; // keep the order: later messages wait for this one
                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }
}
