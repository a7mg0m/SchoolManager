namespace SchoolManager.Notification.Domain
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }

    public sealed record MessageSentEvent(
        Guid MessageId,
        Guid ChatRoomId,
        Guid SenderId,
        IReadOnlyCollection<Guid> RecipientIds,
        DateTime OccurredOn) : IDomainEvent;

    public sealed record MessageDeliveredEvent(
        Guid MessageId,
        Guid ChatRoomId,
        Guid UserId,
        DateTime OccurredOn) : IDomainEvent;

    public sealed record MessageSeenEvent(
        Guid MessageId,
        Guid ChatRoomId,
        Guid UserId,
        DateTime OccurredOn) : IDomainEvent;
}
