namespace SchoolManager.Identity.Application
{
    /// <summary>A menu item with permission flags and its children (permission matrix / my menu).</summary>
    public class PermissionMenuItemDto
    {
        public Guid MenuItemId { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }

        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }

        public List<PermissionMenuItemDto> Children { get; set; } = new();
    }
}
