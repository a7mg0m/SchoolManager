using AutoMapper;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
	public class NotificationTargetMappingProfile : Profile
	{
		public NotificationTargetMappingProfile()
		{
			CreateMap<NotificationTarget, NotificationTargetDto>();
		}
	}
}
