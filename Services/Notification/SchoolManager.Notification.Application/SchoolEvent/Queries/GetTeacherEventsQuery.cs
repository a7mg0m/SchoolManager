using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
    /// <summary>Events for a teacher (teacher app): for everyone, or for teachers including this one.</summary>
    /// <param name="AcademicYearId">Year chosen in the app; null = events of every year</param>
    /// <param name="SemesterId">Semester chosen in the app; null = events of every semester</param>
    public record GetTeacherEventsQuery(Guid TeacherId, Guid? AcademicYearId = null, Guid? SemesterId = null) : IQuery<List<ChildEventDto>>;
}
