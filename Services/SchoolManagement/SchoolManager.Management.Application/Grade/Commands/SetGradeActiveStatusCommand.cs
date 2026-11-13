using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record SetGradeActiveStatusCommand(Guid Id, bool IsActive) : ICommand<bool>;
}
