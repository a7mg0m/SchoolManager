using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class IsChatParticipantQueryHandler(IChatRoomRepository rooms) : IQueryHandler<IsChatParticipantQuery, bool>
    {
        public async Task<bool> Handle(IsChatParticipantQuery query, CancellationToken cancellationToken)
        {
            var room = await rooms.GetByIdWithParticipantsAsync(query.RoomId, cancellationToken);
            return room is not null && room.HasActiveParticipant(query.UserId);
        }
    }
}
