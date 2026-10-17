using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class HomeworkPublishRepository : BaseRepository<HomeworkPublish>, IHomeworkPublishRepository
    {
        public HomeworkPublishRepository(ExamsHomeworkDbContext context) : base(context)
        {
        }
    }
}
