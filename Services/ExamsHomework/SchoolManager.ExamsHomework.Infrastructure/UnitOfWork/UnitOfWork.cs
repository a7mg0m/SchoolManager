using BuildingBlocks;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ExamsHomeworkDbContext _context;

        public UnitOfWork(ExamsHomeworkDbContext context)
        {
            _context = context;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
