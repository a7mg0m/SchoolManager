using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
	public class GetNotificationTargetsByNotificationIdQueryHandler(INotificationTargetRepository repository, IMapper mapper) : IQueryHandler<GetNotificationTargetsByNotificationIdQuery, List<NotificationTargetDto>>
	{
		public async Task<List<NotificationTargetDto>> Handle(GetNotificationTargetsByNotificationIdQuery request, CancellationToken cancellationToken)
		{
			var items = await repository.SearchAsync(x => x.NotificationId == request.NotificationId);
			return mapper.Map<List<NotificationTargetDto>>(items);
		}
	}
}
