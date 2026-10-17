using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record LoginCommand(string LoginName, string Password) : ICommand<LoginResultDto>;
}
