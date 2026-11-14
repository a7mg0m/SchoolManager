using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Builds the menu tree with permission flags.</summary>
    internal static class PermissionTreeBuilder
    {
        public readonly record struct Flags(bool CanView, bool CanAdd, bool CanEdit, bool CanDelete)
        {
            public static readonly Flags None = new(false, false, false, false);
            public static readonly Flags All = new(true, true, true, true);
        }

        /// <param name="all">Every menu item.</param>
        /// <param name="flagsOf">Permission flags of an item.</param>
        /// <param name="activeOnly">Leave out inactive items and everything under them.</param>
        /// <param name="visible">When set, only these items are included (plus nothing else).</param>
        public static List<PermissionMenuItemDto> Build(
            IReadOnlyList<MenuItem> all,
            Func<Guid, Flags> flagsOf,
            bool activeOnly,
            ISet<Guid>? visible = null)
        {
            var ids = all.Select(m => m.Id).ToHashSet();
            // Items whose parent is missing are shown at the top level
            var children = all.ToLookup(m => m.ParentId is Guid pid && ids.Contains(pid) ? pid : (Guid?)null);

            List<PermissionMenuItemDto> BuildLevel(Guid? parentId, HashSet<Guid> path) =>
                children[parentId]
                    .Where(m => !activeOnly || m.IsActive)
                    .Where(m => visible is null || visible.Contains(m.Id))
                    .Where(m => !path.Contains(m.Id)) // guards against bad data loops
                    .OrderBy(m => m.Order)
                    .ThenBy(m => m.Name.English)
                    .Select(m =>
                    {
                        var f = flagsOf(m.Id);
                        return new PermissionMenuItemDto
                        {
                            MenuItemId = m.Id,
                            NameAr = m.Name.Arabic,
                            NameEn = m.Name.English,
                            Icon = m.Icon,
                            Route = m.Route,
                            Order = m.Order,
                            IsActive = m.IsActive,
                            CanView = f.CanView,
                            CanAdd = f.CanAdd,
                            CanEdit = f.CanEdit,
                            CanDelete = f.CanDelete,
                            Children = BuildLevel(m.Id, new HashSet<Guid>(path) { m.Id })
                        };
                    })
                    .ToList();

            return BuildLevel(null, new HashSet<Guid>());
        }

        /// <summary>The given items plus all their ancestors (so a visible page also shows its menu group).</summary>
        public static HashSet<Guid> WithAncestors(IReadOnlyList<MenuItem> all, IEnumerable<Guid> itemIds)
        {
            var byId = all.ToDictionary(m => m.Id);
            var result = new HashSet<Guid>();

            foreach (var id in itemIds)
            {
                var current = (Guid?)id;
                while (current is Guid cid && byId.TryGetValue(cid, out var item) && result.Add(cid))
                    current = item.ParentId;
            }
            return result;
        }
    }
}
