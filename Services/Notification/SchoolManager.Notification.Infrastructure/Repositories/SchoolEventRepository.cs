using Microsoft.EntityFrameworkCore;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class SchoolEventRepository : BaseRepository<SchoolEvent>, ISchoolEventRepository
    {
        public SchoolEventRepository(NotificationDbContext context) : base(context)
        {
        }

        public override Task<SchoolEvent?> GetByIdAsync(Guid id)
            => _dbSet.Include(x => x.Targets).FirstOrDefaultAsync(x => x.Id == id);

        public override async Task<List<SchoolEvent>> GetAllAsync()
            => await _dbSet.Include(x => x.Targets).ToListAsync();
    }
}
