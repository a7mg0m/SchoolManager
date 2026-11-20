namespace SchoolManager.Management.Application
{
    /// <summary>A student in one of the logged-in teacher's classes (teacher app: my classes, exam scores).</summary>
    public class TeacherStudentDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        /// <summary>Male / Female</summary>
        public string Gender { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
    }
}
