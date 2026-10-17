using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetTimetableByIdQuery(Guid Id) : ICommand<TimetableDto?>;
}
