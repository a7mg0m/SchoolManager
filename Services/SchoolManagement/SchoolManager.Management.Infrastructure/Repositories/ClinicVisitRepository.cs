using Microsoft.EntityFrameworkCore;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class ClinicVisitRepository : BaseRepository<ClinicVisit>, IClinicVisitRepository
	{
		public ClinicVisitRepository(ManagementDbContext context) : base(context)
		{
		}

		public Task<ClinicVisit?> GetByIdWithMedicationsAsync(Guid id)
			=> _dbSet.Include(x => x.Medications).FirstOrDefaultAsync(x => x.Id == id);

		public async Task<List<ClinicVisit>> GetByStudentIdAsync(Guid studentId)
			=> await _dbSet.Include(x => x.Medications).Where(x => x.StudentId == studentId).ToListAsync();

		public override async Task<List<ClinicVisit>> GetAllAsync()
			=> await _dbSet.Include(x => x.Medications).ToListAsync();
	}
}
