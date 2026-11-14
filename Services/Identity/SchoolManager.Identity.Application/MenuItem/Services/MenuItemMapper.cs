using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Builds flat DTOs and the menu tree from the full list of menu items (one query, no lazy loading).</summary>
    internal static class MenuItemMapper
    {
        public static List<MenuItemDto> ToDtos(IReadOnlyList<MenuItem> all, IEnumerable<MenuItem>? only = null)
        {
            var byId = all.ToDictionary(m => m.Id);
            var childCounts = all.Where(m => m.ParentId != null)
                .GroupBy(m => m.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            return (only ?? all)
                .Select(m =>
                {
                    var parent = m.ParentId is Guid pid && byId.TryGetValue(pid, out var p) ? p : null;
                    return new MenuItemDto
                    {
                        Id = m.Id,
                        NameAr = m.Name.Arabic,
                        NameEn = m.Name.English,
                        Icon = m.Icon,
                        Route = m.Route,
                        Order = m.Order,
                        IsActive = m.IsActive,
                        ParentId = m.ParentId,
                        ParentNameAr = parent?.Name.Arabic,
                        ParentNameEn = parent?.Name.English,
                        ChildrenCount = childCounts.GetValueOrDefault(m.Id)
                    };
                })
                .ToList();
        }

        /// <summary>
        /// Nested menu. With activeOnly, inactive items and their whole sub-tree are left out.
        /// Items whose parent is missing are shown at the top level.
        /// </summary>
        public static List<MenuItemTreeDto> BuildTree(IReadOnlyList<MenuItem> all, bool activeOnly)
        {
            var ids = all.Select(m => m.Id).ToHashSet();
            var children = all.ToLookup(m => m.ParentId is Guid pid && ids.Contains(pid) ? pid : (Guid?)null);

            List<MenuItemTreeDto> Build(Guid? parentId, HashSet<Guid> path) =>
                children[parentId]
                    .Where(m => !activeOnly || m.IsActive)
                    .Where(m => !path.Contains(m.Id)) // guards against bad data loops
                    .OrderBy(m => m.Order)
                    .ThenBy(m => m.Name.English)
                    .Select(m => new MenuItemTreeDto
                    {
                        Id = m.Id,
                        NameAr = m.Name.Arabic,
                        NameEn = m.Name.English,
                        Icon = m.Icon,
                        Route = m.Route,
                        Order = m.Order,
                        IsActive = m.IsActive,
                        Children = Build(m.Id, new HashSet<Guid>(path) { m.Id })
                    })
                    .ToList();

            return Build(null, new HashSet<Guid>());
        }

        /// <summary>True when <paramref name="candidateParentId"/> is the item itself or one of its descendants.</summary>
        public static bool WouldCreateLoop(IReadOnlyList<MenuItem> all, Guid itemId, Guid candidateParentId)
        {
            var byId = all.ToDictionary(m => m.Id);
            var current = (Guid?)candidateParentId;
            var visited = new HashSet<Guid>();

            while (current is Guid id && visited.Add(id))
            {
                if (id == itemId)
                    return true;
                current = byId.TryGetValue(id, out var node) ? node.ParentId : null;
            }
            return false;
        }
    }
}
