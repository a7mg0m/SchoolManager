using Microsoft.EntityFrameworkCore;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class NotificationRepository : BaseRepository<Domain.Notification>, INotificationRepository
    {
        public NotificationRepository(NotificationDbContext context) : base(context)
        {
        }

        public override Task<Domain.Notification?> GetByIdAsync(Guid id)
            => _dbSet.Include(x => x.Targets).FirstOrDefaultAsync(x => x.Id == id);

        public override async Task<List<Domain.Notification>> GetAllAsync()
            => await _dbSet.Include(x => x.Targets).ToListAsync();
    }
}
