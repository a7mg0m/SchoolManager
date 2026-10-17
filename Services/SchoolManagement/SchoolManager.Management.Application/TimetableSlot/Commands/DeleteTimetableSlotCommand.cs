using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteTimetableSlotCommand(Guid Id) : ICommand<bool>;
}
