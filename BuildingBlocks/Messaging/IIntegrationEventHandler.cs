namespace BuildingBlocks.Messaging
{
    /// <summary>Handles one integration event received from Kafka (resolved in its own DI scope).</summary>
    public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
    {
        Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
    }
}
