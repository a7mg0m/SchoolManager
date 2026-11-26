using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Teachers of a parent's active children (parent app: chat contacts). The controller passes the parent id from
    /// the login token.
    /// - With <paramref name="StudentId"/> (the child chosen in the app): only the teachers who teach that child's
    ///   section in its timetable of the academic year / semester chosen in the app (the year defaults to the
    ///   student's, the semester to the one running today). Not the parent's child: no teachers.
    /// - Without it: the teachers of the subjects of each child's grade (linked to the subject, or teaching it in a
    ///   timetable) — used to check that a chat partner is allowed.
    /// </summary>
    public record GetMyTeachersQuery(
        Guid ParentId,
        Guid? StudentId = null,
        Guid? AcademicYearId = null,
        Guid? SemesterId = null) : IQuery<List<ParentTeacherDto>>;
}
