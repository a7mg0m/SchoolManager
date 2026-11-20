namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// Children of the logged-in parent, from the Management service (Attendance does not know who a student's parent is).
    /// </summary>
    public interface IParentChildrenService
    {
        /// <summary>
        /// Ids of the parent's children, asked with the parent's own token
        /// (the "Authorization" header value of the current request).
        /// Null when the Management service could not answer.
        /// </summary>
        Task<IReadOnlyCollection<Guid>?> GetMyChildrenIdsAsync(string authorization, CancellationToken cancellationToken = default);
    }
}
