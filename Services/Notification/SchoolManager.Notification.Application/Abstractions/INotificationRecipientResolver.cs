using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// Who receives a notification: everyone of some kinds (a notification without targets), and / or these users
    /// (Identity user ids: parent id for parents, teacher id for teachers).
    /// </summary>
    public sealed record NotificationRecipients(IReadOnlyCollection<DeviceUserType> EveryoneOf, IReadOnlyCollection<Guid> UserIds)
    {
        public bool IsEmpty => EveryoneOf.Count == 0 && UserIds.Count == 0;
    }

    /// <summary>Turns a notification's targets (grade / class / teacher / student) into the users to push to</summary>
    public interface INotificationRecipientResolver
    {
        Task<NotificationRecipients> ResolveAsync(Domain.Notification notification, CancellationToken cancellationToken = default);
    }

    /// <summary>Sends the push of a notification / a chat message; failures are logged and never thrown</summary>
    public interface INotificationPushService
    {
        /// <summary>Pushes the notification to its recipients and marks it pushed (the caller saves)</summary>
        Task PushNotificationAsync(Domain.Notification notification, CancellationToken cancellationToken = default);

        /// <summary>Pushes a chat message to the room's other participants</summary>
        Task PushChatMessageAsync(ChatRoom room, ChatMessage message, string senderName, CancellationToken cancellationToken = default);
    }
}
