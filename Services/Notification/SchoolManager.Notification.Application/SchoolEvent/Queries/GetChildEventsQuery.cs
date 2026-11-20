using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// Events that concern a parent's child: for everyone, its stage, grade or section, or the child itself.
    /// The controller has already checked the child belongs to the parent.
    /// </summary>
    /// <param name="AcademicYearId">Year chosen in the app; null = events of every year</param>
    /// <param name="SemesterId">Semester chosen in the app; null = events of every semester</param>
    public record GetChildEventsQuery(ParentChild Child, Guid? AcademicYearId = null, Guid? SemesterId = null) : IQuery<List<ChildEventDto>>;
}
