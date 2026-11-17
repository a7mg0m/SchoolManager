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

        /// <summary>Subject names by id (Arabic, English); empty when they could not be loaded.</summary>
        Task<IReadOnlyDictionary<Guid, (string Ar, string En)>> GetSubjectNamesAsync(string authorization, CancellationToken cancellationToken = default);
    }
}
