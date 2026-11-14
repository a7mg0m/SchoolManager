using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Messaging.Kafka
{
    /// <summary>
    /// Background service that reads <typeparamref name="TEvent"/> messages from one topic and passes each
    /// to <see cref="IIntegrationEventHandler{TEvent}"/> in a new DI scope.
    /// The offset is committed only after the handler succeeds (at-least-once delivery), so handlers must be
    /// idempotent. A message that keeps failing is logged and skipped after MaxHandlerRetries, so one bad
    /// message cannot block the topic. If Kafka is not reachable the service keeps retrying in the background.
    /// </summary>
    public sealed class KafkaConsumerService<TEvent> : BackgroundService where TEvent : IntegrationEvent
    {
        private readonly string _topic;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly KafkaSettings _settings;
        private readonly ILogger<KafkaConsumerService<TEvent>> _logger;

        public KafkaConsumerService(
            string topic,
            IServiceScopeFactory scopeFactory,
            IOptions<KafkaSettings> options,
            ILogger<KafkaConsumerService<TEvent>> logger)
        {
            _topic = topic;
            _scopeFactory = scopeFactory;
            _settings = options.Value;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
            // Consume() blocks, so run on its own thread and don't delay the host start-up
            => Task.Factory.StartNew(() => ConsumeLoop(stoppingToken), stoppingToken,
                TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

        private async Task ConsumeLoop(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                ClientId = _settings.ClientId,
                GroupId = _settings.GroupId ?? throw new InvalidOperationException("Kafka:GroupId is required for consumers"),
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                // Create the topic if it does not exist yet (the producer may not have sent anything)
                AllowAutoCreateTopics = true
            };

            using var consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, error) => _logger.LogWarning("Kafka consumer error ({Topic}): {Reason}", _topic, error.Reason))
                .Build();

            consumer.Subscribe(_topic);
            _logger.LogInformation("Kafka consumer started for {Topic} (group {GroupId})", _topic, config.GroupId);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    ConsumeResult<string, string>? result;
                    try
                    {
                        result = consumer.Consume(stoppingToken);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogWarning(ex, "Kafka consume failed ({Topic}): {Reason}", _topic, ex.Error.Reason);
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                    if (result?.Message is null)
                        continue;

                    await HandleWithRetriesAsync(result, stoppingToken);
                    consumer.Commit(result);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            finally
            {
                consumer.Close();
            }
        }

        private async Task HandleWithRetriesAsync(ConsumeResult<string, string> result, CancellationToken stoppingToken)
        {
            TEvent? integrationEvent;
            try
            {
                integrationEvent = MessagingJson.Deserialize<TEvent>(result.Message.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Skipping unreadable message at {TopicPartitionOffset}", result.TopicPartitionOffset);
                return;
            }

            if (integrationEvent is null)
            {
                _logger.LogError("Skipping empty message at {TopicPartitionOffset}", result.TopicPartitionOffset);
                return;
            }

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<TEvent>>();
                    await handler.HandleAsync(integrationEvent, stoppingToken);
                    return;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (attempt < _settings.MaxHandlerRetries)
                {
                    _logger.LogWarning(ex, "Handling {EventType} {EventId} failed (attempt {Attempt}), retrying",
                        typeof(TEvent).Name, integrationEvent.EventId, attempt);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, 2 * attempt)), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Giving up on {EventType} {EventId} at {TopicPartitionOffset} after {Attempts} attempts",
                        typeof(TEvent).Name, integrationEvent.EventId, result.TopicPartitionOffset, attempt);
                    return;
                }
            }
        }
    }
}
