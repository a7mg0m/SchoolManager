namespace BuildingBlocks.Messaging
{
    /// <summary>
    /// Base of every message sent between services through Kafka.
    /// EventId lets consumers ignore duplicates; OccurredOn is UTC.
    /// </summary>
    public abstract record IntegrationEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    }
}
