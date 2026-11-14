namespace BuildingBlocks.Messaging.Kafka
{
    /// <summary>"Kafka" section of appsettings.json.</summary>
    public sealed class KafkaSettings
    {
        public const string SectionName = "Kafka";

        /// <summary>e.g. "localhost:9092"</summary>
        public string BootstrapServers { get; set; } = "localhost:9092";

        /// <summary>Identifies this service in broker logs.</summary>
        public string ClientId { get; set; } = "schoolmanager";

        /// <summary>Consumer group (consumers only). Each service uses its own group.</summary>
        public string? GroupId { get; set; }

        /// <summary>Retries of a failing message before it is skipped (consumers only).</summary>
        public int MaxHandlerRetries { get; set; } = 5;
    }
}
