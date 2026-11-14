namespace SchoolManager.Identity.Application
{
    public class CreateMenuItemDto
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        /// <summary>Position among siblings; null puts the item last.</summary>
        public int? Order { get; set; }
        /// <summary>Parent menu item; null for a top-level item.</summary>
        public Guid? ParentId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
