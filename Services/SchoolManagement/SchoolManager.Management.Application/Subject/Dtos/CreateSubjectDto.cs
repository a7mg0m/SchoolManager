namespace SchoolManager.Management.Application
{
    public class CreateSubjectDto
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public Guid StageId { get; set; }
        public Guid GradeId { get; set; }
        public List<Guid> TeacherIds { get; set; } = new();

    }
}
