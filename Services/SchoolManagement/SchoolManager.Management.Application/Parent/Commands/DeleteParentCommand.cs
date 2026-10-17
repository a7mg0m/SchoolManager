using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteParentCommand(Guid Id) : ICommand<bool>;
}
