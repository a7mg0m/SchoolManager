using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    /// <summary>Changes the logged-in user's password after checking the current one.</summary>
    public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<bool>;
}
