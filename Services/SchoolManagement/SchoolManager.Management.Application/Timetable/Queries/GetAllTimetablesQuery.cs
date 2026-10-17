using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllTimetablesQuery() : ICommand<List<TimetableDto>>;
}
