using BuildingBlocks;

namespace SchoolManager.Notification.Domain
{
    public interface IUserDeviceTokenRepository : IRepository<UserDeviceToken>
    {
        Task<UserDeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>Tokens of these users (read-only)</summary>
        Task<List<UserDeviceToken>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);

        /// <summary>Tokens of every user of these kinds (a notification for everyone; read-only)</summary>
        Task<List<UserDeviceToken>> GetByUserTypesAsync(IEnumerable<DeviceUserType> userTypes, CancellationToken cancellationToken = default);

        /// <summary>Deletes tokens Firebase rejected (uninstalled app, expired); returns how many were deleted</summary>
        Task<int> DeleteByTokensAsync(IEnumerable<string> tokens, CancellationToken cancellationToken = default);
    }
}
