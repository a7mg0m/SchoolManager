using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class HomeworkQuestionRepository : BaseRepository<HomeworkQuestion>, IHomeworkQuestionRepository
    {
        public HomeworkQuestionRepository(ExamsHomeworkDbContext context) : base(context)
        {
        }
    }
}
