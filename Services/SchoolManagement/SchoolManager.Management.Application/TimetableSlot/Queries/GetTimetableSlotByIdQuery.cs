using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetTimetableSlotByIdQuery(Guid Id) : ICommand<TimetableSlotDto?>;
}
