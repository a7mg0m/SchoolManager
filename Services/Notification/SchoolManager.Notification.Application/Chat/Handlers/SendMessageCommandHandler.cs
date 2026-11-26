using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class SendMessageCommandHandler(
        IChatRoomRepository rooms,
        IChatMessageRepository messages,
        IUserProfileService userProfiles,
        IUnitOfWork unitOfWork,
        INotificationPushService push,
        IChatRealtimeNotifier realtime)
        : ICommandHandler<SendMessageCommand, ChatMessageDto>
    {
        public async Task<ChatMessageDto> Handle(SendMessageCommand command, CancellationToken cancellationToken)
        {
            var room = await rooms.GetByIdWithParticipantsAsync(command.RoomId, cancellationToken)
                ?? throw new KeyNotFoundException($"Chat room {command.RoomId} not found");

            if (!room.HasActiveParticipant(command.SenderId))
                throw new UnauthorizedAccessException("Sender is not a participant of this chat room.");

            var audit = new AuditInfo(DateTime.UtcNow, command.SenderId);
            var message = room.SendMessage(
                command.SenderId,
                command.Content,
                command.Attachments?.Select(a => a.ToDomain()),
                audit);

            await messages.AddAsync(message);
            await rooms.UpdateAsync(room);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var names = await userProfiles.GetDisplayNamesAsync(new[] { command.SenderId }, cancellationToken);
            var dto = message.ToDto(command.SenderId, names);

            // Live to the open apps of everyone in the room (never fails the send)
            var participants = room.Participants.Where(p => p.IsActive).Select(p => p.UserId).Distinct().ToList();
            await realtime.MessageSentAsync(participants, dto, cancellationToken);

            // Push to the other participants (never fails the send; errors are logged)
            await push.PushChatMessageAsync(room, message, names.GetValueOrDefault(command.SenderId) ?? string.Empty, cancellationToken);

            return dto;
        }
    }
}
