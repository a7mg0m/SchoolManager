using AutoMapper;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
	public class EventTargetMappingProfile : Profile
	{
		public EventTargetMappingProfile()
		{
			CreateMap<EventTarget, EventTargetDto>();
		}
	}
}
