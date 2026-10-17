using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public class RoleRepository : BaseRepository<Role>, IRoleRepository
    {
        public RoleRepository(IdentityDbContext context) : base(context)
        {
        }
    }
}
