using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// The teacher's lessons in all their classes (the controller passes the teacher id from the login token).
    /// AcademicYearId / SemesterId: chosen in the app; null = the current year / the semester running today.
    /// </summary>
    public record GetMyTimetableQuery(Guid TeacherId, DateTime Now, Guid? AcademicYearId = null, Guid? SemesterId = null)
        : IQuery<TeacherTimetableDto>;
}
