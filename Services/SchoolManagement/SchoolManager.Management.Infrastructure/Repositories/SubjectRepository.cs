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

		public async Task<List<Subject>> GetForTeacherAsync(Guid teacherId, CancellationToken cancellationToken = default)
			=> await _dbSet
				.AsNoTracking()
				.Include(x => x.Grade)
				.Where(x => x.SubjectTeachers.Any(t => t.TeacherId == teacherId)
					|| _context.TimetableSlots.Any(slot => slot.SubjectId == x.Id && slot.TeacherId == teacherId))
				.ToListAsync(cancellationToken);

		public override async Task<List<Subject>> GetAllAsync()
			=> await _dbSet.Include(x => x.SubjectTeachers).ToListAsync();
	}
}
