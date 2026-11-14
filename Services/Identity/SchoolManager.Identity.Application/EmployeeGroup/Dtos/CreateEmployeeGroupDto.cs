namespace SchoolManager.Identity.Application
{
    public class CreateEmployeeGroupDto
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
