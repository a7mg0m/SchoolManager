using BuildingBlocks;

namespace SchoolManager.Identity.Domain
{
    /// <summary>
    /// What the employees of one group may do on one menu item (page).
    /// Add / Edit / Delete imply View. A group without a row for an item has no access to it.
    /// </summary>
    public class GroupPermission : BaseEntity
    {
        public Guid EmployeeGroupId { get; private set; }
        public EmployeeGroup? EmployeeGroup { get; private set; }

        public Guid MenuItemId { get; private set; }
        public MenuItem? MenuItem { get; private set; }

        public bool CanView { get; private set; }
        public bool CanAdd { get; private set; }
        public bool CanEdit { get; private set; }
        public bool CanDelete { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        private GroupPermission()
        {
        }

        public GroupPermission(Guid employeeGroupId, Guid menuItemId, bool canView, bool canAdd, bool canEdit, bool canDelete, AuditInfo auditInfo)
        {
            EmployeeGroupId = employeeGroupId;
            MenuItemId = menuItemId;
            SetFlags(canView, canAdd, canEdit, canDelete);
            AuditInfo = auditInfo;
        }

        public void Update(bool canView, bool canAdd, bool canEdit, bool canDelete, AuditInfo auditInfo)
        {
            SetFlags(canView, canAdd, canEdit, canDelete);
            AuditInfo = auditInfo;
        }

        /// <summary>True when the flags differ from the given ones (avoids needless updates).</summary>
        public bool Differs(bool canView, bool canAdd, bool canEdit, bool canDelete)
        {
            var view = canView || canAdd || canEdit || canDelete;
            return CanView != view || CanAdd != canAdd || CanEdit != canEdit || CanDelete != canDelete;
        }

        private void SetFlags(bool canView, bool canAdd, bool canEdit, bool canDelete)
        {
            CanAdd = canAdd;
            CanEdit = canEdit;
            CanDelete = canDelete;
            // You cannot add / edit / delete on a page you cannot see
            CanView = canView || canAdd || canEdit || canDelete;
        }
    }
}
