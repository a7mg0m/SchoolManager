using Microsoft.Extensions.Logging;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// Targets → users. Same rules as the apps' notification lists (see NotificationRepository.GetForStudentAsync /
    /// GetForTeacherAsync / GetForDriverAsync): no targets = every parent, teacher and driver; a bus → its driver; a class (section) / a grade without a class / a
    /// student → the parents of those students; a teacher → the teacher. User ids: parent id / teacher id.
    /// </summary>
    public class NotificationRecipientResolver(IManagementClient management, ILogger<NotificationRecipientResolver> logger)
        : INotificationRecipientResolver
    {
        public async Task<NotificationRecipients> ResolveAsync(Domain.Notification notification, CancellationToken cancellationToken = default)
        {
            var targets = notification.Targets;
            if (targets.Count == 0)
                return new NotificationRecipients(new[] { DeviceUserType.Parent, DeviceUserType.Teacher, DeviceUserType.Driver }, Array.Empty<Guid>());

            var userIds = new HashSet<Guid>(targets.Where(t => t.TeacherId is not null).Select(t => t.TeacherId!.Value));
            // A bus's driver: the driver's user id is the bus id
            userIds.UnionWith(targets.Where(t => t.BusId is not null).Select(t => t.BusId!.Value));

            // Class rows also carry their grade: a grade counts only on a row without a class
            var sectionIds = targets.Where(t => t.SectionId is not null).Select(t => t.SectionId!.Value).Distinct().ToList();
            var gradeIds = targets.Where(t => t.SectionId is null && t.GradeId is not null).Select(t => t.GradeId!.Value).Distinct().ToList();
            var studentIds = targets.Where(t => t.StudentId is not null).Select(t => t.StudentId!.Value).Distinct().ToList();

            if (sectionIds.Count + gradeIds.Count + studentIds.Count > 0)
            {
                var parents = await management.GetParentIdsAsync(notification.AcademicYearId, gradeIds, sectionIds, studentIds, cancellationToken);
                if (parents is null)
                    logger.LogWarning("Push of notification {NotificationId}: Management did not answer, parents are not notified", notification.Id);
                else
                    userIds.UnionWith(parents);
            }

            return new NotificationRecipients(Array.Empty<DeviceUserType>(), userIds.ToList());
        }
    }

    public class NotificationPushService(
        INotificationRecipientResolver resolver,
        IUserDeviceTokenRepository tokens,
        IPushNotificationSender sender,
        ILogger<NotificationPushService> logger)
        : INotificationPushService
    {
        /// <summary>Body length of a chat message push</summary>
        public const int ChatBodyMaxLength = 100;

        public async Task PushNotificationAsync(Domain.Notification notification, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!sender.IsEnabled)
                {
                    // Firebase is not configured / did not start: left "not pushed", so the background job sends it
                    // once Firebase works (the service log says why push is disabled)
                    logger.LogWarning("Notification {NotificationId} not pushed: push notifications are disabled", notification.Id);
                    return;
                }

                var recipients = await resolver.ResolveAsync(notification, cancellationToken);
                var devices = new List<UserDeviceToken>();
                if (recipients.EveryoneOf.Count > 0)
                    devices.AddRange(await tokens.GetByUserTypesAsync(recipients.EveryoneOf, cancellationToken));
                if (recipients.UserIds.Count > 0)
                    devices.AddRange(await tokens.GetByUserIdsAsync(recipients.UserIds, cancellationToken));

                var message = new PushMessage(
                    notification.Title,
                    Shorten(notification.Description, 200),
                    new Dictionary<string, string>
                    {
                        ["type"] = "notification",
                        ["notificationId"] = notification.Id.ToString(),
                        ["notificationType"] = ((int)notification.NotificationType).ToString(),
                        ["route"] = $"/notifications/{notification.Id}"
                    });

                await SendAsync(devices, message, cancellationToken);
                notification.MarkPushSent(DateTime.UtcNow);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Push of notification {NotificationId} failed", notification.Id);
            }
        }

        public async Task PushChatMessageAsync(ChatRoom room, ChatMessage message, string senderName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!sender.IsEnabled) return;

                var others = room.Participants
                    .Where(p => p.IsActive && p.UserId != message.SenderId)
                    .Select(p => p.UserId)
                    .Distinct()
                    .ToList();
                if (others.Count == 0) return;

                var devices = await tokens.GetByUserIdsAsync(others, cancellationToken);
                var push = new PushMessage(
                    string.IsNullOrWhiteSpace(senderName) ? "رسالة جديدة" : senderName,
                    // A picture / video / voice note without words: say what it is
                    Shorten(ChatMedia.PreviewAr(message.Content, message.Attachments.FirstOrDefault()?.ContentType), ChatBodyMaxLength),
                    new Dictionary<string, string>
                    {
                        ["type"] = "chat",
                        ["roomId"] = room.Id.ToString(),
                        ["messageId"] = message.Id.ToString(),
                        ["senderId"] = message.SenderId.ToString(),
                        ["route"] = $"/chat/{room.Id}"
                    });

                await SendAsync(devices, push, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Push of chat message {MessageId} failed", message.Id);
            }
        }

        /// <summary>Sends to the devices' tokens and deletes the ones Firebase rejected for good</summary>
        private async Task SendAsync(List<UserDeviceToken> devices, PushMessage message, CancellationToken cancellationToken)
        {
            var list = devices.Select(d => d.Token).Distinct().ToList();
            if (list.Count == 0) return;

            var result = await sender.SendAsync(list, message, cancellationToken);
            if (result.DeadTokens.Count > 0)
            {
                var deleted = await tokens.DeleteByTokensAsync(result.DeadTokens, cancellationToken);
                logger.LogInformation("Push: {Deleted} dead device token(s) deleted", deleted);
            }
            logger.LogInformation("Push \"{Title}\": {Success} delivered, {Failure} failed", message.Title, result.SuccessCount, result.FailureCount);
        }

        private static string Shorten(string? text, int max)
        {
            var value = (text ?? string.Empty).Trim();
            return value.Length <= max ? value : value[..(max - 1)] + "…";
        }
    }
}
