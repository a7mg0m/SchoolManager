using Microsoft.EntityFrameworkCore;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class TimetableRepository : BaseRepository<Timetable>, ITimetableRepository
	{
		public TimetableRepository(ManagementDbContext context) : base(context)
		{
		}

		public override Task<Timetable?> GetByIdAsync(Guid id)
			=> _dbSet.Include(x => x.Slots).FirstOrDefaultAsync(x => x.Id == id);

		public override async Task<List<Timetable>> GetAllAsync()
			=> await _dbSet.Include(x => x.Slots).ToListAsync();
	}
}
