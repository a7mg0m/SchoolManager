using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Active students of a class (section) of a grade the teacher teaches; null when the teacher does not teach
    /// that class's grade (the controller passes the teacher id from the login token).
    /// </summary>
    public record GetMySectionStudentsQuery(Guid TeacherId, Guid SectionId) : IQuery<List<TeacherStudentDto>?>;
}
