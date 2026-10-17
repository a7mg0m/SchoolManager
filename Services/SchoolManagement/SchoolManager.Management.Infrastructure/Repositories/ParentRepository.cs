using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class ParentRepository : BaseRepository<Parent>, IParentRepository
	{
		public ParentRepository(ManagementDbContext context) : base(context)
		{
		}
	}
}
