using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
	/// <summary>All notifications (with targets), newest first.</summary>
	public class GetAllNotificationsQueryHandler(INotificationRepository repository, IMapper mapper) : IQueryHandler<GetAllNotificationsQuery, List<NotificationDto>>
	{
		public async Task<List<NotificationDto>> Handle(GetAllNotificationsQuery request, CancellationToken cancellationToken)
		{
			var items = await repository.GetAllAsync();
			var ordered = items
				.OrderByDescending(x => x.SentOn)
				.ThenByDescending(x => x.AuditInfo.CreatedOn)
				.ToList();
			return mapper.Map<List<NotificationDto>>(ordered);
		}
	}

	public class GetNotificationByIdQueryHandler(INotificationRepository repository, IMapper mapper) : IQueryHandler<GetNotificationByIdQuery, NotificationDto?>
	{
		public async Task<NotificationDto?> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await repository.GetByIdAsync(request.Id);
			return entity is null ? null : mapper.Map<NotificationDto>(entity);
		}
	}
}
