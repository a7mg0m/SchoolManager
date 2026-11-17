using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Re-sends every parent to the Identity service (creates missing logins, updates the rest).
    /// Returns the number of parents queued.
    /// </summary>
    public record SyncParentAccountsCommand() : ICommand<int>;
}
