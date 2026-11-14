using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record SetMenuItemActiveStatusCommand(Guid Id, bool IsActive) : ICommand<bool>;
}
