using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class UpdateMessageStatusCommandHandler(IMessageStatusRepository statuses, IUnitOfWork unitOfWork)
        : ICommandHandler<UpdateMessageStatusCommand>
    {
        public async Task<Unit> Handle(UpdateMessageStatusCommand command, CancellationToken cancellationToken)
        {
            var items = await statuses.GetForUserAsync(command.RoomId, command.UserId, command.MessageIds, cancellationToken);
            var audit = new AuditInfo(DateTime.UtcNow, command.UserId);

            foreach (var status in items)
            {
                if (command.Status == MessageDeliveryStatus.Seen) status.MarkSeen(audit);
                else if (command.Status == MessageDeliveryStatus.Delivered) status.MarkDelivered(audit);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
