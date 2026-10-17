using BuildingBlocks;

namespace SchoolManager.Management.Infrastructure
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly ManagementDbContext _context;

		public UnitOfWork(ManagementDbContext context)
		{
			_context = context;
		}

		public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		{
			return _context.SaveChangesAsync(cancellationToken);
		}
	}
}
