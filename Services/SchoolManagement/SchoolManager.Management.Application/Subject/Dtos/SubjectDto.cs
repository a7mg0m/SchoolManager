namespace SchoolManager.Management.Application
{
    public class SubjectDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public Guid GradeId { get; set; }
        public List<Guid> TeacherIds { get; set; } = new();
        public bool IsActive { get; set; }
    }
}
