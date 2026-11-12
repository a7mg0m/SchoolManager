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
	}
}
