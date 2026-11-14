namespace SchoolManager.Identity.Application
{
    /// <summary>Active status is changed with activate / deactivate.</summary>
    public class UpdateMenuItemDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public int Order { get; set; }
        public Guid? ParentId { get; set; }
    }
}
