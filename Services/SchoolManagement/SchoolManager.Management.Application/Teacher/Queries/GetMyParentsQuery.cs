using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Parents of a teacher's students, each with those children (the controller passes the teacher id from the
    /// login token).
    /// - Default (teacher app "parents" page): the active students in the classes of the grades the teacher teaches.
    /// - <paramref name="FromTimetable"/> (teacher app chat): only the classes the teacher teaches in the timetable of
    ///   <paramref name="AcademicYearId"/> / <paramref name="SemesterId"/> (null: the semester running today; a grade
    ///   without a timetable for it uses its whole-year one). No year: the default above.
    /// </summary>
    public record GetMyParentsQuery(
        Guid TeacherId,
        bool FromTimetable = false,
        Guid? AcademicYearId = null,
        Guid? SemesterId = null) : IQuery<List<TeacherParentDto>>;
}
