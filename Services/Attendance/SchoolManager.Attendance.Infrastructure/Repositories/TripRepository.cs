using Microsoft.EntityFrameworkCore;
using SchoolManager.Attendance.Domain;
using System.Linq.Expressions;

namespace SchoolManager.Attendance.Infrastructure
{
    public class TripRepository : BaseRepository<Trip>, ITripRepository
    {
        public TripRepository(AttendanceDbContext context) : base(context)
        {
        }

        // Always with the students: boarding / drop-off / absent and "already on this trip" need them
        public override Task<Trip?> GetByIdAsync(Guid id)
            => _dbSet.Include(x => x.Students).FirstOrDefaultAsync(x => x.Id == id);

        public override async Task<List<Trip>> GetAllAsync()
            => await _dbSet.Include(x => x.Students).ToListAsync();

        public override async Task<List<Trip>> SearchAsync(Expression<Func<Trip, bool>> predicate)
            => await _dbSet.Include(x => x.Students).Where(predicate).ToListAsync();
    }
}
