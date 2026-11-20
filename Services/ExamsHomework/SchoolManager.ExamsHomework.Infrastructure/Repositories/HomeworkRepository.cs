using Microsoft.EntityFrameworkCore;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class HomeworkRepository : BaseRepository<Homework>, IHomeworkRepository
    {
        public HomeworkRepository(ExamsHomeworkDbContext context) : base(context)
        {
        }

        public async Task<List<Homework>> GetForTeacherAsync(Guid teacherId, CancellationToken cancellationToken = default)
            => await _dbSet
                .AsNoTracking()
                .Include(h => h.HomeworkQuestions)
                .Where(h => h.TeacherId == teacherId)
                .OrderByDescending(h => h.DueDate)
                .ToListAsync(cancellationToken);

        public Task<Homework?> GetWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default)
            => _dbSet.Include(h => h.HomeworkQuestions).FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        public async Task<List<(Homework Homework, DateTime PublishedOn)>> GetPublishedForSectionAsync(
            Guid academicYearId, Guid sectionId, CancellationToken cancellationToken = default)
        {
            var rows = await (
                    from p in _context.Set<HomeworkPublish>().AsNoTracking()
                    join h in _dbSet.AsNoTracking().Include(h => h.HomeworkQuestions) on p.HomeworkId equals h.Id
                    where p.SectionId == sectionId && h.AcademicYearId == academicYearId
                    select new { Homework = h, p.PublishedOn })
                .ToListAsync(cancellationToken);

            // One row per homework (published once per class)
            return rows
                .GroupBy(r => r.Homework.Id)
                .Select(g => (g.First().Homework, g.Min(r => r.PublishedOn)))
                .ToList();
        }
    }
}
