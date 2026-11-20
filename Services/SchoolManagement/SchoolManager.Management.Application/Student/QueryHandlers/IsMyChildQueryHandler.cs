using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class IsMyChildQueryHandler(IStudentRepository students) : IQueryHandler<IsMyChildQuery, bool>
    {
        public async Task<bool> Handle(IsMyChildQuery query, CancellationToken cancellationToken)
        {
            var student = await students.GetByIdAsync(query.StudentId);
            return student is not null && student.ParentId == query.ParentId;
        }
    }
}
