using Microsoft.EntityFrameworkCore;
using SchoolManager.Attendance.Domain;
using System.Linq.Expressions;

namespace SchoolManager.Attendance.Infrastructure
{
    public class BusRouteRepository : BaseRepository<BusRoute>, IBusRouteRepository
    {
        public BusRouteRepository(AttendanceDbContext context) : base(context)
        {
        }

        // Always with the stops: SetStops replaces the loaded list and routes are shown with their stops
        public override Task<BusRoute?> GetByIdAsync(Guid id)
            => _dbSet.Include(x => x.Stops).FirstOrDefaultAsync(x => x.Id == id);

        public override async Task<List<BusRoute>> GetAllAsync()
            => await _dbSet.Include(x => x.Stops).ToListAsync();

        public override async Task<List<BusRoute>> SearchAsync(Expression<Func<BusRoute, bool>> predicate)
            => await _dbSet.Include(x => x.Stops).Where(predicate).ToListAsync();
    }
}
