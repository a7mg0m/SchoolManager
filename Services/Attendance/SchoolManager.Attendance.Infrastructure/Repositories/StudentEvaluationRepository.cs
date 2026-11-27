using Microsoft.EntityFrameworkCore;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class StudentEvaluationRepository : BaseRepository<StudentEvaluation>, IStudentEvaluationRepository
    {
        public StudentEvaluationRepository(AttendanceDbContext context) : base(context)
        {
        }

        public async Task<List<StudentEvaluation>> GetBetweenAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
            => await _dbSet.AsNoTracking()
                .Where(e => e.Date >= from && e.Date <= to)
                .OrderByDescending(e => e.Date)
                .ToListAsync(cancellationToken);
    }
}
