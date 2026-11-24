namespace SchoolManager.Notification.Application
{
    /// <summary>A push notification: title and text shown by the phone, and data the app reads (e.g. where to open)</summary>
    public sealed record PushMessage(string Title, string Body, IReadOnlyDictionary<string, string> Data);

    /// <summary>How a push went: delivered count and the tokens Firebase rejected for good (to delete)</summary>
    public sealed record PushSendResult(int SuccessCount, int FailureCount, IReadOnlyList<string> DeadTokens)
    {
        public static readonly PushSendResult Empty = new(0, 0, Array.Empty<string>());
    }

    /// <summary>Sends push notifications to device tokens (Firebase Cloud Messaging)</summary>
    public interface IPushNotificationSender
    {
        /// <summary>False when push is not configured (development without Firebase): sending does nothing</summary>
        bool IsEnabled { get; }

        Task<PushSendResult> SendAsync(IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken cancellationToken = default);
    }
}
