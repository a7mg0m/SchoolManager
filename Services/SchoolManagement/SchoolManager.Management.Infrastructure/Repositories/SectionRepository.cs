using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class SectionRepository : BaseRepository<Section>, ISectionRepository
	{
		public SectionRepository(ManagementDbContext context) : base(context)
		{
		}
	}
}
