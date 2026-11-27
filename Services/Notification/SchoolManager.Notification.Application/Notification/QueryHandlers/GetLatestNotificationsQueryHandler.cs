using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>Home dashboard: the latest school-office notifications (newest first)</summary>
    public record GetLatestNotificationsQuery(int Take) : IQuery<List<NotificationDto>>;

    public class GetLatestNotificationsQueryHandler(INotificationRepository repository, IMapper mapper)
        : IQueryHandler<GetLatestNotificationsQuery, List<NotificationDto>>
    {
        public async Task<List<NotificationDto>> Handle(GetLatestNotificationsQuery request, CancellationToken cancellationToken)
        {
            var take = Math.Clamp(request.Take, 1, 200);
            return mapper.Map<List<NotificationDto>>(await repository.GetLatestAdminAsync(take, cancellationToken));
        }
    }
}
