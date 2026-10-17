using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class ExamRepository : BaseRepository<Exam>, IExamRepository
    {
        public ExamRepository(ExamsHomeworkDbContext context) : base(context)
        {
        }
    }
}
