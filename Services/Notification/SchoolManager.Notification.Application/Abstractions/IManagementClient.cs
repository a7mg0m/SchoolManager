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
        /// </summary>
        Task<IReadOnlyList<ChatContact>?> GetChatContactsAsync(string authorization, bool asTeacher, CancellationToken cancellationToken = default);
    }
}
