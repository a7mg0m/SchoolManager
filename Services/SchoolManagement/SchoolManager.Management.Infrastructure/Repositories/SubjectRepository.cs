using Microsoft.EntityFrameworkCore;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class SubjectRepository : BaseRepository<Subject>, ISubjectRepository
	{
		public SubjectRepository(ManagementDbContext context) : base(context)
		{
		}

		public Task<Subject?> GetByIdWithTeachersAsync(Guid id)
			=> _dbSet.Include(x => x.SubjectTeachers).FirstOrDefaultAsync(x => x.Id == id);

		public override async Task<List<Subject>> GetAllAsync()
			=> await _dbSet.Include(x => x.SubjectTeachers).ToListAsync();
	}
}
