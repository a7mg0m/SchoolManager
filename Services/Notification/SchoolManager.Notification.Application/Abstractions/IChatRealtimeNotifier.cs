using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// Live chat updates to the apps that are connected (SignalR hub /hubs/chat), so the chat list and the open chat
    /// change without a refresh. Phones that are not connected get the push notification instead.
    /// Implementations never throw: a failed live update must not fail the send / the status change.
    /// </summary>
    public interface IChatRealtimeNotifier
    {
        /// <summary>A new message: to every participant (the sender too, for their other devices)</summary>
        Task MessageSentAsync(IEnumerable<Guid> userIds, ChatMessageDto message, CancellationToken cancellationToken = default);

        /// <summary>Messages delivered to / seen by <paramref name="byUserId"/>: to the other participants (their ticks)</summary>
        Task StatusChangedAsync(
            IEnumerable<Guid> userIds,
            Guid roomId,
            Guid byUserId,
            IReadOnlyCollection<Guid> messageIds,
            MessageDeliveryStatus status,
            CancellationToken cancellationToken = default);
    }
}
