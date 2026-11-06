using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class SendMessageCommandHandler(
        IChatRoomRepository rooms,
        IChatMessageRepository messages,
        IUserProfileService userProfiles,
        IUnitOfWork unitOfWork)
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
            return message.ToDto(command.SenderId, names);
        }
    }
}
