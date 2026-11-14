using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record SetEmployeeGroupActiveStatusCommand(Guid Id, bool IsActive) : ICommand<bool>;
}
