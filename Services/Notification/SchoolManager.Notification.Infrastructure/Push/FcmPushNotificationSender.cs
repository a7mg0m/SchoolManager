using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.Infrastructure
{
    /// <summary>
    /// Firebase Cloud Messaging. The Firebase app is created once from the service-account JSON file in
    /// "Firebase:CredentialsPath" (never committed). Without it push is disabled: a warning is logged and sending does
    /// nothing, so development runs without Firebase. Registered as a singleton.
    /// </summary>
    public class FcmPushNotificationSender : IPushNotificationSender
    {
        /// <summary>FCM limit for one multicast</summary>
        private const int BatchSize = 500;

        private readonly ILogger<FcmPushNotificationSender> _logger;
        private readonly FirebaseMessaging? _messaging;

        public FcmPushNotificationSender(IConfiguration configuration, ILogger<FcmPushNotificationSender> logger)
        {
            _logger = logger;
            var path = configuration["Firebase:CredentialsPath"];
            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.LogWarning("Push notifications are disabled: Firebase:CredentialsPath is not set");
                return;
            }
            if (!File.Exists(path))
            {
                _logger.LogWarning("Push notifications are disabled: the Firebase credentials file {Path} was not found", path);
                return;
            }

            try
            {
                var app = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions
                {
#pragma warning disable CS0618 // FromFile: the file is our own service-account key (not user input)
                    Credential = GoogleCredential.FromFile(path)
#pragma warning restore CS0618
                });
                _messaging = FirebaseMessaging.GetMessaging(app);
                _logger.LogInformation("Push notifications are enabled (Firebase project {ProjectId})", app.Options.ProjectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Push notifications are disabled: Firebase could not start");
            }
        }

        public bool IsEnabled => _messaging is not null;

        public async Task<PushSendResult> SendAsync(IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken cancellationToken = default)
        {
            if (_messaging is null || tokens.Count == 0)
                return PushSendResult.Empty;

            int success = 0, failure = 0;
            var dead = new List<string>();

            foreach (var batch in tokens.Distinct().Chunk(BatchSize))
            {
                var multicast = new MulticastMessage
                {
                    Tokens = batch,
                    Notification = new FirebaseAdmin.Messaging.Notification { Title = message.Title, Body = message.Body },
                    Data = message.Data,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        // Same channel as the apps create
                        Notification = new AndroidNotification { ChannelId = "school_notifications", Sound = "default" }
                    },
                    Apns = new ApnsConfig { Aps = new Aps { Sound = "default" } }
                };

                try
                {
                    var response = await _messaging.SendEachForMulticastAsync(multicast, cancellationToken);
                    success += response.SuccessCount;
                    failure += response.FailureCount;

                    for (var i = 0; i < response.Responses.Count; i++)
                    {
                        var error = response.Responses[i].Exception?.MessagingErrorCode;
                        // The app was uninstalled / the token expired or is malformed: delete it
                        if (error is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                            dead.Add(batch[i]);
                    }
                }
                catch (FirebaseMessagingException ex)
                {
                    failure += batch.Length;
                    _logger.LogError(ex, "Firebase rejected a push batch of {Count} token(s)", batch.Length);
                }
            }

            return new PushSendResult(success, failure, dead);
        }
    }
}
