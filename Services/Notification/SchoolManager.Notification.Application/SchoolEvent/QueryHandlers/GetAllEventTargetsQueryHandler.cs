using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
	public class GetAllEventTargetsQueryHandler(IEventTargetRepository repository, IMapper mapper) : IQueryHandler<GetAllEventTargetsQuery, List<EventTargetDto>>
	{
		public async Task<List<EventTargetDto>> Handle(GetAllEventTargetsQuery request, CancellationToken cancellationToken)
		{
			var items = await repository.GetAllAsync();
			return mapper.Map<List<EventTargetDto>>(items);
		}
	}
}
