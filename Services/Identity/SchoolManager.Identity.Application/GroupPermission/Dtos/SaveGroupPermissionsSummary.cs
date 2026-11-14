namespace SchoolManager.Identity.Application
{
    /// <summary>Result of saving the permissions of a group.</summary>
    public record SaveGroupPermissionsSummary(int Created, int Updated, int Removed);
}
