namespace SchoolManager.Management.Application
{
    /// <summary>A subject the teacher teaches, its grade and the grade's classes.</summary>
    public class TeacherSubjectDto
    {
        public Guid SubjectId { get; set; }
        public string SubjectNameAr { get; set; } = string.Empty;
        public string SubjectNameEn { get; set; } = string.Empty;
        public Guid GradeId { get; set; }
        /// <summary>Stage of the grade (exams need it)</summary>
        public Guid StageId { get; set; }
        public string GradeNameAr { get; set; } = string.Empty;
        public string GradeNameEn { get; set; } = string.Empty;
        public List<TeacherSectionDto> Sections { get; set; } = new();
    }
}
