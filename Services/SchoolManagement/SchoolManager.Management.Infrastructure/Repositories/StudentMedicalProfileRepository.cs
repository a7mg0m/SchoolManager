using Microsoft.EntityFrameworkCore;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class StudentMedicalProfileRepository : BaseRepository<StudentMedicalProfile>, IStudentMedicalProfileRepository
	{
		public StudentMedicalProfileRepository(ManagementDbContext context) : base(context)
		{
		}

		public Task<StudentMedicalProfile?> GetByStudentIdAsync(Guid studentId)
			=> _dbSet.FirstOrDefaultAsync(x => x.StudentId == studentId);
	}
}
