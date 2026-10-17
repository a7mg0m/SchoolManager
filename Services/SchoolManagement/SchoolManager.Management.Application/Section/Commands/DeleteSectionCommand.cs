using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteSectionCommand(Guid Id) : ICommand<bool>;
}
