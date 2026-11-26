using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Does the parent still have a child at the school (current, not graduated / transferred)? Identity asks it when a
    /// parent signs in: only such parents may log in to the parent app.
    /// </summary>
    public record HasCurrentStudentsQuery(Guid ParentId) : IQuery<bool>;

    public class HasCurrentStudentsQueryHandler(IStudentRepository students) : IQueryHandler<HasCurrentStudentsQuery, bool>
    {
        public async Task<bool> Handle(HasCurrentStudentsQuery query, CancellationToken cancellationToken)
        {
            if (query.ParentId == Guid.Empty) return false;
            var current = await students.SearchAsync(s =>
                s.ParentId == query.ParentId && s.IsActive && s.Status == StudentStatus.Current);
            return current.Count > 0;
        }
    }
}
