using Microsoft.EntityFrameworkCore;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class ExamRepository : BaseRepository<Exam>, IExamRepository
    {
        public ExamRepository(ExamsHomeworkDbContext context) : base(context)
        {
        }

        public async Task<List<Exam>> GetForStudentAsync(Guid academicYearId, Guid gradeId, Guid sectionId, Guid studentId, CancellationToken cancellationToken = default)
            => await _dbSet
                .AsNoTracking()
                .Where(e => e.AcademicYearId == academicYearId
                            && e.GradeId == gradeId
                            && (e.SectionId == null || e.SectionId == sectionId))
                // Only this student's score, never the classmates'
                .Include(e => e.ExamResults.Where(r => r.StudentId == studentId))
                .ToListAsync(cancellationToken);
    }
}
