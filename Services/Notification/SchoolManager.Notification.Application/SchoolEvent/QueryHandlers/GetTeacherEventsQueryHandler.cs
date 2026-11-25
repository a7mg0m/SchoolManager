using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class GetTeacherEventsQueryHandler(ISchoolEventRepository repository) : IQueryHandler<GetTeacherEventsQuery, List<ChildEventDto>>
    {
        public async Task<List<ChildEventDto>> Handle(GetTeacherEventsQuery query, CancellationToken cancellationToken)
        {
            var events = await repository.GetForTeacherAsync(query.AcademicYearId, query.SemesterId, query.TeacherId, cancellationToken);

            return events
                .OrderBy(e => e.EventDate).ThenBy(e => e.StartTime)
                .Select(e => new ChildEventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    ImageUrl = string.IsNullOrWhiteSpace(e.ImageUrl) ? DefaultImages.Event : e.ImageUrl,
                    EventFor = e.EventFor
                })
                .ToList();
        }
    }
}
