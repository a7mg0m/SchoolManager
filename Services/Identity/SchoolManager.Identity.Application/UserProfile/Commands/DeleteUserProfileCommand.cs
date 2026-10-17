using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record DeleteUserProfileCommand(Guid Id) : ICommand<bool>;
}
