using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteStageCommand(Guid Id) : ICommand<bool>;
}
