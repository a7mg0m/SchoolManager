using BuildingBlocks.Messaging;

namespace SchoolManager.Management.Infrastructure
{
    /// <summary>
    /// Adds the event to the outbox table of the current DbContext. It is written by the handler's
    /// own SaveChanges, in the same transaction as the business change.
    /// </summary>
    public class IntegrationEventOutbox : IIntegrationEventOutbox
    {
        private readonly ManagementDbContext _context;

        public IntegrationEventOutbox(ManagementDbContext context)
        {
            _context = context;
        }

        public void Enqueue<TEvent>(string topic, string key, TEvent integrationEvent) where TEvent : IntegrationEvent
        {
            _context.OutboxMessages.Add(new OutboxMessage
            {
                Id = integrationEvent.EventId,
                Topic = topic,
                Key = key,
                EventType = typeof(TEvent).Name,
                Payload = MessagingJson.Serialize(integrationEvent),
                OccurredOn = integrationEvent.OccurredOn
            });
        }
    }
}
