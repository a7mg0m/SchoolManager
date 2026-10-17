using BuildingBlocks.CQRS;


namespace SchoolManager.Identity.Application
{
    public record DeleteUserCommand(Guid Id) : ICommand<bool>;

}
