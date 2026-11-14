namespace BuildingBlocks.Messaging
{
    /// <summary>Sends a message to the message broker (Kafka).</summary>
    public interface IEventPublisher
    {
        /// <param name="topic">Topic name, see <see cref="Topics"/>.</param>
        /// <param name="key">Message key; messages with the same key keep their order.</param>
        /// <param name="payload">JSON payload.</param>
        /// <param name="eventType">Event type name, sent as the "event-type" header.</param>
        Task PublishAsync(string topic, string key, string payload, string eventType, CancellationToken cancellationToken = default);
    }
}
