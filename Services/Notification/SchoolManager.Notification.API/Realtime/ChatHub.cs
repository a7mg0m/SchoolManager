using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SchoolManager.Notification.Application;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.API.Realtime
{
    /// <summary>
    /// Live chat (SignalR) at /hubs/chat (the gateway forwards /hubs/**). The apps connect with their JWT
    /// (?access_token= on the WebSocket) and only listen; sending stays on the REST API (POST /api/chat/send-message).
    /// Server → app events (camelCase JSON):
    ///  - "chatMessage": a ChatMessageDto (id, chatRoomId, senderId, senderName, content, sentOn, attachments, status)
    ///  - "chatStatus":  { roomId, userId, messageIds, status } — messages delivered (2) / seen (3) by userId
    /// Every connection of a user gets them (SignalR user = the JWT "sub").
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        public const string Path = "/hubs/chat";
        public const string MessageEvent = "chatMessage";
        public const string StatusEvent = "chatStatus";
    }

    /// <summary>SignalR user id = the JWT subject (the same id the REST API uses as the current user)</summary>
    public class SubjectUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection) =>
            connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>Sends the live chat events through <see cref="ChatHub"/>; failures are logged, never thrown</summary>
    public class SignalRChatNotifier(IHubContext<ChatHub> hub, ILogger<SignalRChatNotifier> logger) : IChatRealtimeNotifier
    {
        public async Task MessageSentAsync(IEnumerable<Guid> userIds, ChatMessageDto message, CancellationToken cancellationToken = default)
        {
            var users = Users(userIds);
            if (users.Count == 0) return;
            try
            {
                await hub.Clients.Users(users).SendAsync(ChatHub.MessageEvent, message, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Live update of chat message {MessageId} failed", message.Id);
            }
        }

        public async Task StatusChangedAsync(
            IEnumerable<Guid> userIds,
            Guid roomId,
            Guid byUserId,
            IReadOnlyCollection<Guid> messageIds,
            MessageDeliveryStatus status,
            CancellationToken cancellationToken = default)
        {
            var users = Users(userIds);
            if (users.Count == 0 || messageIds.Count == 0) return;
            try
            {
                await hub.Clients.Users(users).SendAsync(
                    ChatHub.StatusEvent,
                    new { roomId, userId = byUserId, messageIds, status = (int)status },
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Live update of chat status in room {RoomId} failed", roomId);
            }
        }

        private static List<string> Users(IEnumerable<Guid> ids) =>
            ids.Where(id => id != Guid.Empty).Select(id => id.ToString()).Distinct().ToList();
    }
}
