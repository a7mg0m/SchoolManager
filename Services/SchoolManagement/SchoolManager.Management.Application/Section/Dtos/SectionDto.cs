namespace SchoolManager.Management.Application
{
    public class SectionDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public Guid GradeId { get; set; }
        public bool IsActive { get; set; }
    }
}
