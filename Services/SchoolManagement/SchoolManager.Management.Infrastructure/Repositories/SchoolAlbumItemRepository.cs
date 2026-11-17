using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
    public class SchoolAlbumItemRepository : BaseRepository<SchoolAlbumItem>, ISchoolAlbumItemRepository
    {
        public SchoolAlbumItemRepository(ManagementDbContext context) : base(context)
        {
        }
    }
}
