using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>Subjects / classes of a teacher (the controller passes the teacher id from the login token).</summary>
    /// <param name="AcademicYearId">Year chosen in the app; null = the current (active) academic year</param>
    /// <param name="SemesterId">Semester chosen in the app (must be in that year); null = the running one</param>
    public record GetMyClassesQuery(Guid TeacherId, Guid? AcademicYearId = null, Guid? SemesterId = null) : IQuery<TeacherClassesDto>;
}
