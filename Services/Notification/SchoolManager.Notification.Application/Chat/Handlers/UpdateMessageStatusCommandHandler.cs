using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class UpdateMessageStatusCommandHandler(
        IMessageStatusRepository statuses,
        IChatRoomRepository rooms,
        IUnitOfWork unitOfWork,
        IChatRealtimeNotifier realtime)
        : ICommandHandler<UpdateMessageStatusCommand>
    {
        public async Task<Unit> Handle(UpdateMessageStatusCommand command, CancellationToken cancellationToken)
        {
            var items = await statuses.GetForUserAsync(command.RoomId, command.UserId, command.MessageIds, cancellationToken);
            var audit = new AuditInfo(DateTime.UtcNow, command.UserId);

            // Only the messages that really move forward (Sent → Delivered → Seen) are announced
            var changed = new List<Guid>();
            foreach (var status in items)
            {
                var before = status.Status;
                if (command.Status == MessageDeliveryStatus.Seen) status.MarkSeen(audit);
                else if (command.Status == MessageDeliveryStatus.Delivered) status.MarkDelivered(audit);
                if (status.Status != before) changed.Add(status.MessageId);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (changed.Count > 0)
            {
                // The senders' ticks turn grey / blue live
                var room = await rooms.GetByIdWithParticipantsAsync(command.RoomId, cancellationToken);
                if (room is not null)
                {
                    var others = room.Participants
                        .Where(p => p.IsActive && p.UserId != command.UserId)
                        .Select(p => p.UserId)
                        .Distinct()
                        .ToList();
                    await realtime.StatusChangedAsync(others, command.RoomId, command.UserId, changed, command.Status, cancellationToken);
                }
            }

            return Unit.Value;
        }
    }
}
