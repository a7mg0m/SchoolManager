namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Asks the Management service whether a parent still has a child at the school (current, not graduated /
    /// transferred). Only such parents may sign in.
    /// </summary>
    public interface IParentStudentsChecker
    {
        /// <summary>True / false; null when the Management service could not answer.</summary>
        Task<bool?> HasCurrentStudentsAsync(Guid parentId, CancellationToken cancellationToken = default);
    }
}
