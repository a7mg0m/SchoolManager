using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Re-sends every teacher to the Identity service (as Updated events), e.g. to create logins for
    /// teachers that existed before Kafka was added. Returns the number of teachers sent.
    /// </summary>
    public record SyncTeacherAccountsCommand() : ICommand<int>;
}
