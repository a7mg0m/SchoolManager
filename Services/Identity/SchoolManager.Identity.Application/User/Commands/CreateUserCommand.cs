using BuildingBlocks.CQRS;


namespace SchoolManager.Identity.Application
{
    public record CreateUserCommand(
        string LoginName,
        string Email,
        string HashPassword,
        Guid RoleId
    ) : ICommand<Guid>;
}
