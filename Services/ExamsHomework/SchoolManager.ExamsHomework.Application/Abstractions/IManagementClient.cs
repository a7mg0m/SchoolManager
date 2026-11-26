namespace SchoolManager.ExamsHomework.Application
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
        /// A student the logged-in teacher teaches (with grade / class / year); Student null when the teacher does not
        /// teach them; Unavailable when the Management service could not answer.
        /// </summary>
        Task<(ParentChild? Student, bool Unavailable)> GetTaughtStudentAsync(string authorization, Guid studentId, CancellationToken cancellationToken = default);

        /// <summary>Subject names by id (Arabic, English); empty when they could not be loaded.</summary>
        Task<IReadOnlyDictionary<Guid, (string Ar, string En)>> GetSubjectNamesAsync(string authorization, CancellationToken cancellationToken = default);

        /// <summary>Subjects / classes of the logged-in teacher (for the chosen academic year, else the current one); null when the Management service could not answer.</summary>
        /// <summary>
        /// Active students of one of the logged-in teacher's classes; empty when the teacher does not teach that class,
        /// null when the Management service could not answer.
        /// </summary>
        Task<IReadOnlyList<SectionStudent>?> GetMySectionStudentsAsync(string authorization, Guid sectionId, CancellationToken cancellationToken = default);

        Task<TeacherClasses?> GetMyClassesAsync(string authorization, Guid? academicYearId = null, Guid? semesterId = null, CancellationToken cancellationToken = default);
    }
}
