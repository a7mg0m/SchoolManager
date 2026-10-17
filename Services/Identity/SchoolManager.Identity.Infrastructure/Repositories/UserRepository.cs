using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Identity.Domain;
using System.Linq.Expressions;

namespace SchoolManager.Identity.Infrastructure
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        private readonly IdentityDbContext _context;

        public UserRepository(IdentityDbContext context) : base(context)
        {
            _context = context;
        }

     
        public async Task<List<User>> SearchAsync(string searchTerm)
        {
            return await _context.Users
                .Where(u => u.LoginName.Contains(searchTerm) || u.Email.Contains(searchTerm))
                .ToListAsync();
        }

        public async Task<User?> GetByLoginNameAsync(string loginName)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.LoginName == loginName);
        }

        public async Task<List<User>> GetAllNonReservedAsync()
        {
            return await _context.Users.Where(u => !u.IsReserved).ToListAsync();
        }
    }
}
