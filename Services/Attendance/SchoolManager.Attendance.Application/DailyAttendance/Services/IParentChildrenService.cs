namespace SchoolManager.Attendance.Application
{
    /// <summary>One of the parent's children, as Management describes it</summary>
    public sealed record ParentChildInfo(Guid Id, string FullNameAr, string FullNameEn, Guid AcademicYearId);

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

        /// <summary>The parent's children with their names and academic year (null when Management could not answer)</summary>
        Task<IReadOnlyCollection<ParentChildInfo>?> GetMyChildrenAsync(string authorization, CancellationToken cancellationToken = default);
    }
}
