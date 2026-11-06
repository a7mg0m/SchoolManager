using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class CreatePrivateChatCommandHandler(IChatRoomRepository rooms, IUnitOfWork unitOfWork)
        : ICommandHandler<CreatePrivateChatCommand, Guid>
    {
        public async Task<Guid> Handle(CreatePrivateChatCommand command, CancellationToken cancellationToken)
        {
            var existing = await rooms.GetPrivateRoomAsync(command.CurrentUserId, command.OtherUserId, cancellationToken);
            if (existing is not null) return existing.Id;

            var audit = new AuditInfo(DateTime.UtcNow, command.CurrentUserId);
            var room = ChatRoom.CreatePrivate(command.CurrentUserId, command.OtherUserId, audit);

            await rooms.AddAsync(room);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return room.Id;
        }
    }
}
