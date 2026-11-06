using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class GetChatListQueryHandler(
        IChatRoomRepository rooms,
        IChatMessageRepository messages,
        IUserProfileService userProfiles)
        : IQueryHandler<GetChatListQuery, IReadOnlyList<ChatListItemDto>>
    {
        public async Task<IReadOnlyList<ChatListItemDto>> Handle(GetChatListQuery query, CancellationToken cancellationToken)
        {
            var userRooms = await rooms.GetUserRoomsAsync(query.UserId, cancellationToken);
            if (userRooms.Count == 0) return Array.Empty<ChatListItemDto>();

            var roomIds = userRooms.Select(r => r.Id).ToList();
            var lastMessages = await messages.GetLastMessagesAsync(roomIds, cancellationToken);
            var unread = await messages.GetUnreadCountsAsync(query.UserId, roomIds, cancellationToken);

            var otherUserIds = userRooms
                .Where(r => r.Type == ChatRoomType.Private)
                .SelectMany(r => r.Participants)
                .Select(p => p.UserId)
                .Where(id => id != query.UserId)
                .Distinct()
                .ToList();
            var names = await userProfiles.GetDisplayNamesAsync(otherUserIds, cancellationToken);

            return userRooms
                .Select(room =>
                {
                    lastMessages.TryGetValue(room.Id, out var last);
                    return new ChatListItemDto(
                        room.Id,
                        room.Type,
                        ResolveTitle(room, query.UserId, names),
                        last?.Content,
                        last?.SenderId,
                        last?.SentOn ?? room.LastMessageOn,
                        unread.GetValueOrDefault(room.Id));
                })
                .OrderByDescending(x => x.LastMessageOn)
                .ToList();
        }

        private static string ResolveTitle(ChatRoom room, Guid userId, IReadOnlyDictionary<Guid, string> names)
        {
            if (room.Type == ChatRoomType.SectionGroup) return room.Name ?? string.Empty;

            var other = room.Participants.FirstOrDefault(p => p.UserId != userId)?.UserId;
            return other is not null && names.TryGetValue(other.Value, out var name) ? name : string.Empty;
        }
    }
}
