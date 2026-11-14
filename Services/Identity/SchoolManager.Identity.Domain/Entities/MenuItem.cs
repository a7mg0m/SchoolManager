using BuildingBlocks;

namespace SchoolManager.Identity.Domain
{
    /// <summary>
    /// An entry of the application menu (sidebar). Items can be nested: a group (no route)
    /// with child items (with a route).
    /// </summary>
    public class MenuItem : BaseEntity
    {
        public NameLocalized Name { get; private set; }
        /// <summary>Icon CSS class, e.g. "pe-7s-users".</summary>
        public string? Icon { get; private set; }
        /// <summary>Angular route, e.g. "/users/employees". Empty for a group that only has children.</summary>
        public string? Route { get; private set; }
        /// <summary>Position among its siblings (ascending).</summary>
        public int Order { get; private set; }
        public bool IsActive { get; private set; }

        public Guid? ParentId { get; private set; }
        public MenuItem? Parent { get; private set; }
        public List<MenuItem> Children { get; private set; } = new();

        public AuditInfo AuditInfo { get; private set; }

        private MenuItem()
        {
        }

        public MenuItem(NameLocalized name, string? icon, string? route, int order, Guid? parentId, bool isActive, AuditInfo auditInfo)
        {
            Name = name;
            Icon = icon;
            Route = route;
            Order = order;
            ParentId = parentId;
            IsActive = isActive;
            AuditInfo = auditInfo;
        }

        public void Update(NameLocalized name, string? icon, string? route, int order, Guid? parentId, AuditInfo auditInfo)
        {
            Name = name;
            Icon = icon;
            Route = route;
            Order = order;
            ParentId = parentId;
            AuditInfo = auditInfo;
        }

        public void Activate(AuditInfo auditInfo)
        {
            IsActive = true;
            AuditInfo = auditInfo;
        }

        public void Deactivate(AuditInfo auditInfo)
        {
            IsActive = false;
            AuditInfo = auditInfo;
        }
    }
}
