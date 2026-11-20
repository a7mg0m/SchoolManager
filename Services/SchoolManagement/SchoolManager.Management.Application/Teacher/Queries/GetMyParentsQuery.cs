using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Parents of the active students in the classes of the grades a teacher teaches, each with those children
    /// (the controller passes the teacher id from the login token).
    /// </summary>
    public record GetMyParentsQuery(Guid TeacherId) : IQuery<List<TeacherParentDto>>;
}
