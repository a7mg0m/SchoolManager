namespace SchoolManager.Identity.Application
{
    /// <summary>A menu item with its children, ready to render a sidebar.</summary>
    public class MenuItemTreeDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public List<MenuItemTreeDto> Children { get; set; } = new();
    }
}
