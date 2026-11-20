using Microsoft.EntityFrameworkCore;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class StudentVaccinationRepository : BaseRepository<StudentVaccination>, IStudentVaccinationRepository
	{
		public StudentVaccinationRepository(ManagementDbContext context) : base(context)
		{
		}

		public async Task<List<StudentVaccination>> GetByStudentIdAsync(Guid studentId)
			=> await _dbSet.Include(x => x.Vaccine).Where(x => x.StudentId == studentId).ToListAsync();

		public override async Task<List<StudentVaccination>> GetAllAsync()
			=> await _dbSet.Include(x => x.Vaccine).ToListAsync();
	}
}
