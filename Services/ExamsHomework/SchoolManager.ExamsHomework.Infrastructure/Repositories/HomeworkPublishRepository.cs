using Microsoft.EntityFrameworkCore;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class HomeworkPublishRepository : BaseRepository<HomeworkPublish>, IHomeworkPublishRepository
    {
        public HomeworkPublishRepository(ExamsHomeworkDbContext context) : base(context)
        {
        }

        public async Task<List<HomeworkPublish>> GetForHomeworksAsync(IReadOnlyCollection<Guid> homeworkIds, CancellationToken cancellationToken = default)
            => homeworkIds.Count == 0
                ? new List<HomeworkPublish>()
                : await _dbSet.AsNoTracking().Where(p => homeworkIds.Contains(p.HomeworkId)).ToListAsync(cancellationToken);
    }
}
