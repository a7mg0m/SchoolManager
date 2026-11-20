using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class VaccineRepository : BaseRepository<Vaccine>, IVaccineRepository
	{
		public VaccineRepository(ManagementDbContext context) : base(context)
		{
		}
	}
}
