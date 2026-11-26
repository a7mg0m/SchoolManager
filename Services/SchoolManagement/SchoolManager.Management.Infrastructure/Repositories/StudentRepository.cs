using Microsoft.EntityFrameworkCore;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class StudentRepository : BaseRepository<Student>, IStudentRepository
	{
		public StudentRepository(ManagementDbContext context) : base(context)
		{
		}

		public async Task<List<string>> GetExistingNationalIdsAsync(IEnumerable<string> nationalIds, CancellationToken cancellationToken = default)
		{
			var ids = nationalIds
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Select(id => id.Trim())
				.Distinct()
				.ToList();

			if (ids.Count == 0)
				return new List<string>();

			return await _dbSet
				.Where(s => ids.Contains(s.NationalId))
				.Select(s => s.NationalId)
				.ToListAsync(cancellationToken);
		}

		public async Task<List<Student>> GetByParentWithAcademicInfoAsync(Guid parentId, CancellationToken cancellationToken = default)
		{
			return await _dbSet
				.AsNoTracking()
				.Include(s => s.Stage)
				.Include(s => s.Grade)
				.Include(s => s.Section)
				.Where(s => s.ParentId == parentId && s.IsActive)
				.ToListAsync(cancellationToken);
		}

		public async Task<Student?> GetByIdWithAcademicInfoAsync(Guid id, CancellationToken cancellationToken = default)
		{
			return await _dbSet
				.AsNoTracking()
				.Include(s => s.Stage)
				.Include(s => s.Grade)
				.Include(s => s.Section)
				.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
		}

		public async Task<List<Student>> GetByIdsWithParentAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
		{
			if (ids.Count == 0)
				return new List<Student>();

			return await _dbSet
				.AsNoTracking()
				.Include(s => s.Grade)
				.Include(s => s.Section)
				.Include(s => s.Parent)
				.Where(s => ids.Contains(s.Id))
				.ToListAsync(cancellationToken);
		}
	}
}
