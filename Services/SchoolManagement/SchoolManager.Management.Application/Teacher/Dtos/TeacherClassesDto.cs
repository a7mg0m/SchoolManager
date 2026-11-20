namespace SchoolManager.Management.Application
{
    /// <summary>
    /// What the logged-in teacher teaches (subjects with their grade and classes) and the current
    /// academic year / semester, for the teacher app (homework).
    /// </summary>
    public class TeacherClassesDto
    {
        /// <summary>Current academic year (null when none is active)</summary>
        public Guid? AcademicYearId { get; set; }

        /// <summary>Current semester of that year (null when none is active)</summary>
        public Guid? SemesterId { get; set; }

        public List<TeacherSubjectDto> Subjects { get; set; } = new();
    }
}
