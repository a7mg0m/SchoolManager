using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteTimetableCommand(Guid Id) : ICommand<bool>;
}
