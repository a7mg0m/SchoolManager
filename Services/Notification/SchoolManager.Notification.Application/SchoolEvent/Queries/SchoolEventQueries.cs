using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
    public record GetAllSchoolEventsQuery : IQuery<List<SchoolEventDto>>;

    public record GetSchoolEventByIdQuery(Guid Id) : IQuery<SchoolEventDto?>;
}
