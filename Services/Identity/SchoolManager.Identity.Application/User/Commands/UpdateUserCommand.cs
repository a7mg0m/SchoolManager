using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record UpdateUserCommand(
        Guid Id,
        string LoginName,
        string? Email,
        Guid RoleId,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
