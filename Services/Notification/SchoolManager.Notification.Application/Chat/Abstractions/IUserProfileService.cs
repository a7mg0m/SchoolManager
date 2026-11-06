namespace SchoolManager.Notification.Application
{
    /// <summary>Gateway to the Identity Service. The Notification Service never stores user profile data.</summary>
    public interface IUserProfileService
    {
        Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    }
}
