namespace BuildingBlocks.Messaging
{
    /// <summary>
    /// Transactional outbox: Enqueue stores the event in the service's own database as part of the
    /// current unit of work, so it is saved by the same SaveChanges as the business change.
    /// A background publisher then sends it to Kafka. If Kafka is down, nothing is lost.
    /// </summary>
    public interface IIntegrationEventOutbox
    {
        void Enqueue<TEvent>(string topic, string key, TEvent integrationEvent) where TEvent : IntegrationEvent;
    }
}
