using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
	public class GetNotificationTargetByIdQueryHandler(INotificationTargetRepository repository, IMapper mapper) : IQueryHandler<GetNotificationTargetByIdQuery, NotificationTargetDto?>
	{
		public async Task<NotificationTargetDto?> Handle(GetNotificationTargetByIdQuery request, CancellationToken cancellationToken)
		{
			var item = await repository.GetByIdAsync(request.Id);
			return item is null ? null : mapper.Map<NotificationTargetDto>(item);
		}
	}
}
