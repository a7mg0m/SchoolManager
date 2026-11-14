namespace SchoolManager.Identity.Application
{
    /// <summary>A menu item in a flat list (with its parent's name and number of children).</summary>
    public class MenuItemDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public Guid? ParentId { get; set; }
        public string? ParentNameAr { get; set; }
        public string? ParentNameEn { get; set; }
        public int ChildrenCount { get; set; }
    }
}
