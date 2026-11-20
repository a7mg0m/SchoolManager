using BuildingBlocks;

namespace SchoolManager.Attendance.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AttendanceDbContext _context;

        public UnitOfWork(AttendanceDbContext context)
        {
            _context = context;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
