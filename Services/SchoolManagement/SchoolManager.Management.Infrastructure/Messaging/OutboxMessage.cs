namespace SchoolManager.Management.Infrastructure
{
    /// <summary>An integration event waiting to be (or already) published to Kafka.</summary>
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        /// <summary>Event class name, sent as the "event-type" header.</summary>
        public string EventType { get; set; } = string.Empty;
        /// <summary>Event as JSON.</summary>
        public string Payload { get; set; } = string.Empty;
        public DateTime OccurredOn { get; set; }
        /// <summary>Set when Kafka acknowledged the message; null = still pending.</summary>
        public DateTime? ProcessedOn { get; set; }
        public int Attempts { get; set; }
        public string? LastError { get; set; }
    }
}
