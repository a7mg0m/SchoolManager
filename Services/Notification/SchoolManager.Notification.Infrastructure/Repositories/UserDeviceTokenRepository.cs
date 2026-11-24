using Microsoft.EntityFrameworkCore;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class UserDeviceTokenRepository(NotificationDbContext context)
        : BaseRepository<UserDeviceToken>(context), IUserDeviceTokenRepository
    {
        public Task<UserDeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
            _dbSet.FirstOrDefaultAsync(x => x.Token == token, cancellationToken);

        public Task<List<UserDeviceToken>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
        {
            var ids = userIds.Distinct().ToList();
            return ids.Count == 0
                ? Task.FromResult(new List<UserDeviceToken>())
                : _dbSet.AsNoTracking().Where(x => ids.Contains(x.UserId)).ToListAsync(cancellationToken);
        }

        public Task<List<UserDeviceToken>> GetByUserTypesAsync(IEnumerable<DeviceUserType> userTypes, CancellationToken cancellationToken = default)
        {
            var types = userTypes.Distinct().ToList();
            return types.Count == 0
                ? Task.FromResult(new List<UserDeviceToken>())
                : _dbSet.AsNoTracking().Where(x => types.Contains(x.UserType)).ToListAsync(cancellationToken);
        }

        public async Task<int> DeleteByTokensAsync(IEnumerable<string> tokens, CancellationToken cancellationToken = default)
        {
            var list = tokens.Distinct().ToList();
            if (list.Count == 0) return 0;
            return await _dbSet.Where(x => list.Contains(x.Token)).ExecuteDeleteAsync(cancellationToken);
        }
    }
}
