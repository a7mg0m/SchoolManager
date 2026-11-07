using BuildingBlocks;

namespace SchoolManager.Financial.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FinancialDbContext _context;

        public UnitOfWork(FinancialDbContext context)
        {
            _context = context;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
