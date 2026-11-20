namespace SchoolManager.Management.Application
{
    /// <summary>A teacher of the logged-in parent's children (parent app: chat).</summary>
    public class ParentTeacherDto
    {
        public Guid TeacherId { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;

        /// <summary>The subjects this teacher teaches the children ("رياضيات", …)</summary>
        public List<ParentTeacherSubjectDto> Subjects { get; set; } = new();

        /// <summary>The children this teacher teaches</summary>
        public List<ParentTeacherChildDto> Children { get; set; } = new();
    }

    public class ParentTeacherSubjectDto
    {
        public Guid SubjectId { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
    }

    public class ParentTeacherChildDto
    {
        public Guid StudentId { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
    }
}
