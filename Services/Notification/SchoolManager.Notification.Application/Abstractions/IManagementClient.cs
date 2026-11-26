namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// Data this service needs from the Management service, asked with the logged-in user's own token
    /// (the "Authorization" header value of the current request).
    /// </summary>
    public interface IManagementClient
    {
        /// <summary>Children of the logged-in parent; null when the Management service could not answer.</summary>
        Task<IReadOnlyList<ParentChild>?> GetMyChildrenAsync(string authorization, CancellationToken cancellationToken = default);

        /// <summary>
        /// Who the logged-in user may chat with: the parents of a teacher's students, or the teachers of a parent's
        /// children (<paramref name="asTeacher"/>). Null when the Management service could not answer.
        /// A parent with <paramref name="studentId"/> (the child chosen in the app): only the teachers of that child's
        /// section timetable in the academic year / semester chosen in the app.
        /// </summary>
        Task<IReadOnlyList<ChatContact>?> GetChatContactsAsync(
            string authorization,
            bool asTeacher,
            CancellationToken cancellationToken = default,
            Guid? studentId = null,
            Guid? academicYearId = null,
            Guid? semesterId = null);

        /// <summary>
        /// Parents (ids = their Identity user ids) of the active students in these grades / classes and of these students,
        /// for push notifications. Called with a service token (works in background jobs too). Null when Management
        /// could not answer.
        /// </summary>
        Task<IReadOnlyList<Guid>?> GetParentIdsAsync(
            Guid? academicYearId,
            IReadOnlyCollection<Guid> gradeIds,
            IReadOnlyCollection<Guid> sectionIds,
            IReadOnlyCollection<Guid> studentIds,
            CancellationToken cancellationToken = default);
    }
}
