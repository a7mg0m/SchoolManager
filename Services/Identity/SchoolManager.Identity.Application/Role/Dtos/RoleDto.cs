namespace SchoolManager.Identity.Application
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string NameArabic { get; set; } = string.Empty;
        public string NameEnglish { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
