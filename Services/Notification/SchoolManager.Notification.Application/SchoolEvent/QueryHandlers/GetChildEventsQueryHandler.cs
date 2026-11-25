using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class GetChildEventsQueryHandler(ISchoolEventRepository repository) : IQueryHandler<GetChildEventsQuery, List<ChildEventDto>>
    {
        public async Task<List<ChildEventDto>> Handle(GetChildEventsQuery query, CancellationToken cancellationToken)
        {
            var child = query.Child;
            var events = await repository.GetForStudentAsync(
                query.AcademicYearId, query.SemesterId, child.GradeId, child.SectionId, child.Id, cancellationToken);

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
