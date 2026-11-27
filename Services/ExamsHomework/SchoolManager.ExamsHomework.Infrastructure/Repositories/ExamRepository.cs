using Microsoft.EntityFrameworkCore;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class ExamRepository : BaseRepository<Exam>, IExamRepository
    {
        public ExamRepository(ExamsHomeworkDbContext context) : base(context)
        {
        }

        public async Task<List<Exam>> GetForTeacherAsync(Guid teacherId, Guid? academicYearId, Guid? semesterId, CancellationToken cancellationToken = default)
            => await _dbSet
                .AsNoTracking()
                .Where(e => e.TeacherId == teacherId
                            && (academicYearId == null || e.AcademicYearId == academicYearId)
                            && (semesterId == null || e.SemesterId == semesterId))
                .Include(e => e.ExamResults)
                .OrderByDescending(e => e.ExamDate).ThenByDescending(e => e.StartTime)
                .ToListAsync(cancellationToken);

        public async Task<List<Exam>> GetForTeacherClassesAsync(Guid teacherId, IReadOnlyCollection<Guid> subjectIds, IReadOnlyCollection<Guid> gradeIds,
            Guid? academicYearId, Guid? semesterId, CancellationToken cancellationToken = default)
            => await _dbSet
                .AsNoTracking()
                .Where(e => (e.TeacherId == teacherId || (subjectIds.Contains(e.SubjectId) && gradeIds.Contains(e.GradeId)))
                            && (academicYearId == null || e.AcademicYearId == academicYearId)
                            && (semesterId == null || e.SemesterId == semesterId))
                .Include(e => e.ExamResults)
                .OrderByDescending(e => e.ExamDate).ThenByDescending(e => e.StartTime)
                .ToListAsync(cancellationToken);

        public async Task<List<Exam>> GetFinalsWithResultsAsync(Guid academicYearId, Guid gradeId, CancellationToken cancellationToken = default)
            => await _dbSet
                .AsNoTracking()
                .Include(e => e.ExamResults)
                .Where(e => e.AcademicYearId == academicYearId && e.GradeId == gradeId
                    && (e.ExamType == ExamType.Final || e.ExamType == ExamType.SecondFinal))
                .ToListAsync(cancellationToken);

        public async Task<List<ExamResultSummary>> GetResultSummariesAsync(CancellationToken cancellationToken = default)
            => await _dbSet.AsNoTracking()
                .Where(e => e.ExamResults.Any())
                .Select(e => new ExamResultSummary(
                    e.Id,
                    e.ExamResults.Count,
                    e.ExamResults.Count(r => r.Score >= e.SuccessScore),
                    e.ExamResults.Average(r => r.Score)))
                .ToListAsync(cancellationToken);

        public async Task<Exam?> GetWithResultsAsync(Guid id, CancellationToken cancellationToken = default)
            => await _dbSet
                .AsNoTracking()
                .Include(e => e.ExamResults)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

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
