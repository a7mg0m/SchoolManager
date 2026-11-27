using Microsoft.EntityFrameworkCore;
using SchoolManager.Attendance.Domain;
using System.Linq.Expressions;

namespace SchoolManager.Attendance.Infrastructure
{
    public class DailyAttendanceRepository : BaseRepository<DailyAttendance>, IDailyAttendanceRepository
    {
        public DailyAttendanceRepository(AttendanceDbContext context) : base(context)
        {
        }

        // Always with the periods: the day's rules (one row per lesson, recalculated status) need them
        public override Task<DailyAttendance?> GetByIdAsync(Guid id)
            => _dbSet.Include(x => x.Periods).FirstOrDefaultAsync(x => x.Id == id);

        public override async Task<List<DailyAttendance>> GetAllAsync()
            => await _dbSet.Include(x => x.Periods).ToListAsync();

        public async Task<List<DailyAttendanceCounts>> GetReportRowsAsync(DateOnly from, DateOnly to, Guid? academicYearId,
            Guid? gradeId, Guid? sectionId, CancellationToken cancellationToken = default)
            => await _dbSet.AsNoTracking()
                .Where(d => d.Date >= from && d.Date <= to
                    && (academicYearId == null || d.AcademicYearId == academicYearId)
                    && (gradeId == null || d.GradeId == gradeId)
                    && (sectionId == null || d.SectionId == sectionId))
                .OrderBy(d => d.Date)
                .Select(d => new DailyAttendanceCounts(
                    d.StudentId, d.GradeId, d.SectionId, d.AcademicYearId, d.Date, d.Status,
                    d.Periods.Count(p => p.Status == PeriodAttendanceStatus.Present),
                    d.Periods.Count(p => p.Status == PeriodAttendanceStatus.Late),
                    d.Periods.Count(p => p.Status == PeriodAttendanceStatus.Absent),
                    d.Periods.Count(p => p.Status == PeriodAttendanceStatus.Excused)))
                .ToListAsync(cancellationToken);

        public override async Task<List<DailyAttendance>> SearchAsync(Expression<Func<DailyAttendance, bool>> predicate)
            => await _dbSet.Include(x => x.Periods).Where(predicate).ToListAsync();
    }
}
