using BuildingBlocks;

namespace SchoolManager.Identity.Domain
{
    public interface IMenuItemRepository : IRepository<MenuItem>
    {
        /// <summary>All menu items ordered by Order, then English name.</summary>
        Task<List<MenuItem>> GetAllOrderedAsync();

        Task<bool> HasChildrenAsync(Guid id);

        /// <summary>True if a sibling (same parent) already uses this Arabic or English name.</summary>
        Task<bool> NameExistsAsync(Guid? parentId, string nameAr, string nameEn, Guid? excludeId = null);

        /// <summary>Next Order value for a new item under the parent (max + 1, or 1).</summary>
        Task<int> GetNextOrderAsync(Guid? parentId);
    }
}
