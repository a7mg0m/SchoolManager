using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Teachers of a parent's active children: the teachers of the subjects of each child's grade (linked to the
    /// subject, or teaching it in a timetable). The controller passes the parent id from the login token.
    /// </summary>
    public record GetMyTeachersQuery(Guid ParentId) : IQuery<List<ParentTeacherDto>>;
}
