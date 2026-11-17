using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// The whole weekly timetable of a parent's child (the section's slots in its grade's timetable for its
    /// academic year). Returns null when the student does not exist or is not the parent's child.
    /// </summary>
    public record GetChildTimetableQuery(Guid StudentId, Guid ParentId, DateTime Now) : ICommand<ChildTimetableDto?>;
}
