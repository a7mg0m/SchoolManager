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

        public override async Task<List<DailyAttendance>> SearchAsync(Expression<Func<DailyAttendance, bool>> predicate)
            => await _dbSet.Include(x => x.Periods).Where(predicate).ToListAsync();
    }
}
