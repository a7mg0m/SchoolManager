using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record SetStageActiveStatusCommand(Guid Id, bool IsActive, Guid UpdatedBy) : ICommand<bool>;
}
