namespace SchoolManager.Identity.Application
{
    /// <summary>Menu of the logged-in user with what they may do on each page.</summary>
    public class MyMenuDto
    {
        /// <summary>
        /// True for SystemAdmin / Admin users that are not employees: they see the whole active menu
        /// with every permission. Employees get the permissions of their group; other users get no items.
        /// </summary>
        public bool IsFullAccess { get; set; }
        public Guid? EmployeeGroupId { get; set; }
        public List<PermissionMenuItemDto> MenuItems { get; set; } = new();
    }
}
