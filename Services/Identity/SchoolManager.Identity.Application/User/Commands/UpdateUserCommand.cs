using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record UpdateUserCommand(
        Guid Id,
        string LoginName,
        string? Email,
        Guid RoleId
    ) : ICommand<bool>;
}
