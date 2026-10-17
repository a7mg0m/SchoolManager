using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class StageRepository : BaseRepository<Stage>, IStageRepository
	{
		public StageRepository(ManagementDbContext context) : base(context)
		{
		}
	}
}
