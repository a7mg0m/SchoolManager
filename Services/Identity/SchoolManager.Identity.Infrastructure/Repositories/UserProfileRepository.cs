using Microsoft.EntityFrameworkCore;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public class UserProfileRepository : BaseRepository<UserProfile>, IUserProfileRepository
    {
        private readonly IdentityDbContext _context;

        public UserProfileRepository(IdentityDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<UserProfile>> SearchAsync(string searchTerm)
        {
            return await _context.UserProfiles
                .Where(p => p.FirstName.Contains(searchTerm)
                    || p.LastName.Contains(searchTerm)
                    || p.Email.Contains(searchTerm)
                    || p.PhoneNumber.Contains(searchTerm))
                .ToListAsync();
        }
    }
}
