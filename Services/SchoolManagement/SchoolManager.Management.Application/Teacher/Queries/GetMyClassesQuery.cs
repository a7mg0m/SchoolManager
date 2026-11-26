using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>Subjects / classes of a teacher (the controller passes the teacher id from the login token).</summary>
    /// <param name="AcademicYearId">Year chosen in the app; null = the current (active) academic year</param>
    /// <param name="SemesterId">Semester chosen in the app (must be in that year); null = the running one</param>
    /// <param name="FromTimetable">
    /// Teacher app "My classes": only the subjects / classes the teacher has lessons of in that year / semester's
    /// timetable (not every class of the grades of their subjects)
    /// </param>
    public record GetMyClassesQuery(Guid TeacherId, Guid? AcademicYearId = null, Guid? SemesterId = null, bool FromTimetable = false)
        : IQuery<TeacherClassesDto>;
}
