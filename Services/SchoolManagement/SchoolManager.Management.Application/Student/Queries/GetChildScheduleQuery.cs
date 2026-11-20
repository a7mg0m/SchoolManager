using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Lessons of a parent's child for today (or the next school day when none are left today).
    /// Returns null when the student does not exist or is not the parent's child.
    /// </summary>
    /// <param name="AcademicYearId">Year chosen in the app; null = the student's own academic year</param>
    public record GetChildScheduleQuery(Guid StudentId, Guid ParentId, DateTime Now, Guid? AcademicYearId = null, Guid? SemesterId = null) : ICommand<ChildScheduleDto?>;
}
