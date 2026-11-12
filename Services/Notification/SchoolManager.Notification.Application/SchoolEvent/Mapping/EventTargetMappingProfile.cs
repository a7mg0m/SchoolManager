using AutoMapper;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
	public class EventTargetMappingProfile : Profile
	{
		public EventTargetMappingProfile()
		{
			CreateMap<EventTarget, EventTargetDto>();
			CreateMap<Domain.SchoolEvent, SchoolEventDto>()
				.ForMember(d => d.CreatedOn, o => o.MapFrom(s => s.AuditInfo.CreatedOn));
		}
	}
}
