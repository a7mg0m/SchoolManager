namespace SchoolManager.Management.Application
{
    /// <summary>A parent of students in the logged-in teacher's classes (teacher app: parents).</summary>
    public class TeacherParentDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }

        /// <summary>Only the children who are in the teacher's classes</summary>
        public List<TeacherParentChildDto> Children { get; set; } = new();
    }

    public class TeacherParentChildDto
    {
        public Guid StudentId { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public Guid SectionId { get; set; }
        public string SectionNameAr { get; set; } = string.Empty;
        public string SectionNameEn { get; set; } = string.Empty;
        public string GradeNameAr { get; set; } = string.Empty;
        public string GradeNameEn { get; set; } = string.Empty;
    }
}
