using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
    public class SchoolInfoRepository : BaseRepository<SchoolInfo>, ISchoolInfoRepository
    {
        public SchoolInfoRepository(ManagementDbContext context) : base(context)
        {
        }
    }
}
