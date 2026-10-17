using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteSemesterCommand(Guid Id) : ICommand<bool>;
}
