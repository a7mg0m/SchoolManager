using Microsoft.EntityFrameworkCore;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
    public class SchoolInfoRepository : BaseRepository<SchoolInfo>, ISchoolInfoRepository
    {
        public SchoolInfoRepository(ManagementDbContext context) : base(context)
        {
        }

        // The school info is always returned with its album
        public override Task<SchoolInfo?> GetByIdAsync(Guid id)
            => _dbSet.Include(x => x.Album).FirstOrDefaultAsync(x => x.Id == id);

        public override async Task<List<SchoolInfo>> GetAllAsync()
            => await _dbSet.Include(x => x.Album).ToListAsync();
    }
}
