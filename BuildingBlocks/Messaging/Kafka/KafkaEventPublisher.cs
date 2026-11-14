using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Messaging.Kafka
{
    /// <summary>Kafka producer (singleton). Waits for the broker to acknowledge each message.</summary>
    public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaEventPublisher> _logger;

        public KafkaEventPublisher(IOptions<KafkaSettings> options, ILogger<KafkaEventPublisher> logger)
        {
            _logger = logger;
            var settings = options.Value;

            var config = new ProducerConfig
            {
                BootstrapServers = settings.BootstrapServers,
                ClientId = settings.ClientId,
                Acks = Acks.All,
                // No duplicates / reordering when the producer retries
                EnableIdempotence = true,
                MessageTimeoutMs = 10_000
            };

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, error) => _logger.LogWarning("Kafka producer error: {Reason}", error.Reason))
                .Build();
        }

        public async Task PublishAsync(string topic, string key, string payload, string eventType, CancellationToken cancellationToken = default)
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = payload,
                Headers = new Headers { { "event-type", Encoding.UTF8.GetBytes(eventType) } }
            };

            var result = await _producer.ProduceAsync(topic, message, cancellationToken);
            _logger.LogDebug("Published {EventType} to {TopicPartitionOffset}", eventType, result.TopicPartitionOffset);
        }

        public void Dispose()
        {
            try
            {
                _producer.Flush(TimeSpan.FromSeconds(5));
            }
            finally
            {
                _producer.Dispose();
            }
        }
    }
}
