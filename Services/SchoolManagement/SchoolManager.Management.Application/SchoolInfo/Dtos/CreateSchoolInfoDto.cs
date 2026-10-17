namespace SchoolManager.Management.Application
{
    public class CreateSchoolInfoDto
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public byte[]? Logo { get; set; }
    }
}
