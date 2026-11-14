namespace SchoolManager.Identity.Application
{
    /// <summary>The whole menu with the permissions of one employee group (items without a row have all flags false).</summary>
    public class GroupPermissionsDto
    {
        public Guid EmployeeGroupId { get; set; }
        public string EmployeeGroupNameAr { get; set; } = string.Empty;
        public string EmployeeGroupNameEn { get; set; } = string.Empty;
        public List<PermissionMenuItemDto> MenuItems { get; set; } = new();
    }
}
