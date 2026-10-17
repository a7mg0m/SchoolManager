namespace SchoolManager.Management.Application
{
    public class GradeDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public Guid StageId { get; set; }
        public bool IsActive { get; set; }
    }
}
