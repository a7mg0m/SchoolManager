namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Full set of permissions of a group. Menu items not in the list (or with all flags false)
    /// lose their permission.
    /// </summary>
    public class SaveGroupPermissionsDto
    {
        public Guid EmployeeGroupId { get; set; }
        public List<GroupPermissionDto> Permissions { get; set; } = new();
    }
}
