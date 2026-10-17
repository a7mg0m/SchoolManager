using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
	public class GetAllNotificationTargetsQueryHandler(INotificationTargetRepository repository, IMapper mapper) : IQueryHandler<GetAllNotificationTargetsQuery, List<NotificationTargetDto>>
	{
		public async Task<List<NotificationTargetDto>> Handle(GetAllNotificationTargetsQuery request, CancellationToken cancellationToken)
		{
			var items = await repository.GetAllAsync();
			return mapper.Map<List<NotificationTargetDto>>(items);
		}
	}
}
