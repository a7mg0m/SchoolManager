using AutoMapper;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
	public class NotificationTargetMappingProfile : Profile
	{
		public NotificationTargetMappingProfile()
		{
			CreateMap<NotificationTarget, NotificationTargetDto>();
			CreateMap<Domain.Notification, NotificationDto>()
				.ForMember(d => d.CreatedOn, o => o.MapFrom(s => s.AuditInfo.CreatedOn));
		}
	}
}
