namespace SchoolManager.Identity.Application
{
    /// <summary>Permissions of a group on one menu item.</summary>
    public class GroupPermissionDto
    {
        public Guid MenuItemId { get; set; }
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
