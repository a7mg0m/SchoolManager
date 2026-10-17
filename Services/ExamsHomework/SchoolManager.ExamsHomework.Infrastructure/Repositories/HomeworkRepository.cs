using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class HomeworkRepository : BaseRepository<Homework>, IHomeworkRepository
    {
        public HomeworkRepository(ExamsHomeworkDbContext context) : base(context)
        {
        }
    }
}
