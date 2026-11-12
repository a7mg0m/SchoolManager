using Microsoft.EntityFrameworkCore;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class ParentRepository : BaseRepository<Parent>, IParentRepository
	{
		public ParentRepository(ManagementDbContext context) : base(context)
		{
		}

		public Task<Parent?> GetByIdWithStudentsAsync(Guid id)
			=> _dbSet.Include(x => x.Students).FirstOrDefaultAsync(x => x.Id == id);

		public Task<List<Parent>> GetAllWithStudentsAsync()
			=> _dbSet.Include(x => x.Students).ToListAsync();
	}
}
