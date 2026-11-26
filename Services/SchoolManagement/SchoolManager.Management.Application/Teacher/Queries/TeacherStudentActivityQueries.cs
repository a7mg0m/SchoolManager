using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// One student the teacher teaches (their class is in a grade the teacher teaches a subject of, or the teacher has
    /// lessons in that class's timetable); null otherwise. Used by the other services to check a teacher's access
    /// (GET api/teacher/teaches-student/{studentId}).
    /// </summary>
    public record GetTaughtStudentQuery(Guid TeacherId, Guid StudentId) : IQuery<TaughtStudentDto?>;

    /// <summary>
    /// Teacher app "student activity": the student's profile, their class timetable in the academic year / semester
    /// chosen in the app (lesson names for the attendance) and the teachers of their grade with their subjects
    /// (names for the reviews). Null when the teacher does not teach the student.
    /// </summary>
    public record GetTeacherStudentActivityQuery(Guid TeacherId, Guid StudentId, Guid? AcademicYearId, Guid? SemesterId)
        : IQuery<TeacherStudentActivityDto?>;

    public class TaughtStudentDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        /// <summary>Male / Female</summary>
        public string Gender { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public Guid StageId { get; set; }
        public Guid GradeId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid AcademicYearId { get; set; }
        public string StageNameAr { get; set; } = string.Empty;
        public string StageNameEn { get; set; } = string.Empty;
        public string GradeNameAr { get; set; } = string.Empty;
        public string GradeNameEn { get; set; } = string.Empty;
        public string SectionNameAr { get; set; } = string.Empty;
        public string SectionNameEn { get; set; } = string.Empty;
    }

    public class TeacherStudentActivityDto
    {
        public TaughtStudentDto Student { get; set; } = new();

        /// <summary>The class's lessons (same shape as the parent app's child timetable)</summary>
        public ChildTimetableDto Timetable { get; set; } = new();

        /// <summary>Teachers of the student's grade / class with the subjects they teach it</summary>
        public List<ParentTeacherDto> Teachers { get; set; } = new();
    }
}
