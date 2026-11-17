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
    }
}
