using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class CreateGroupChatCommandHandler(IChatRoomRepository rooms, IUnitOfWork unitOfWork)
        : ICommandHandler<CreateGroupChatCommand, Guid>
    {
        public async Task<Guid> Handle(CreateGroupChatCommand command, CancellationToken cancellationToken)
        {
            var audit = new AuditInfo(DateTime.UtcNow, command.TeacherId);
            var parentIds = command.ParentIds.Distinct().Where(id => id != command.TeacherId).ToList();

            var existing = await rooms.GetSectionGroupRoomAsync(command.SectionId, cancellationToken);
            if (existing is not null)
            {
                foreach (var parentId in parentIds) existing.AddParticipant(parentId, audit);
                await rooms.UpdateAsync(existing);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return existing.Id;
            }

            var room = ChatRoom.CreateSectionGroup(command.SectionId, command.Name, command.TeacherId, parentIds, audit);
            await rooms.AddAsync(room);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return room.Id;
        }
    }
}
