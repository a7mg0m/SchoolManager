using Microsoft.EntityFrameworkCore;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public class MenuItemRepository : BaseRepository<MenuItem>, IMenuItemRepository
    {
        public MenuItemRepository(IdentityDbContext context) : base(context)
        {
        }

        public Task<List<MenuItem>> GetAllOrderedAsync()
            => _context.MenuItems
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Name.English)
                .ToListAsync();

        public Task<bool> HasChildrenAsync(Guid id)
            => _context.MenuItems.AnyAsync(m => m.ParentId == id);

        public Task<bool> NameExistsAsync(Guid? parentId, string nameAr, string nameEn, Guid? excludeId = null)
        {
            var ar = nameAr.Trim();
            var en = nameEn.Trim();
            return _context.MenuItems.AnyAsync(m =>
                m.ParentId == parentId &&
                (excludeId == null || m.Id != excludeId) &&
                (m.Name.Arabic == ar || m.Name.English == en));
        }

        public async Task<int> GetNextOrderAsync(Guid? parentId)
        {
            var max = await _context.MenuItems
                .Where(m => m.ParentId == parentId)
                .MaxAsync(m => (int?)m.Order);
            return (max ?? 0) + 1;
        }
    }
}
