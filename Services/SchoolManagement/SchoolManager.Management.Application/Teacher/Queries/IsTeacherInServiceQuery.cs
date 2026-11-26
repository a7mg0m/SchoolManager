using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Is the teacher still in service (exists and service not ended)? Identity asks it when a teacher signs in: only
    /// such teachers may log in to the teacher app.
    /// </summary>
    public record IsTeacherInServiceQuery(Guid TeacherId) : IQuery<bool>;

    public class IsTeacherInServiceQueryHandler(ITeacherRepository teachers) : IQueryHandler<IsTeacherInServiceQuery, bool>
    {
        public async Task<bool> Handle(IsTeacherInServiceQuery query, CancellationToken cancellationToken)
        {
            if (query.TeacherId == Guid.Empty) return false;
            var teacher = await teachers.GetByIdAsync(query.TeacherId);
            return teacher is not null && !teacher.IsServiceEnded;
        }
    }
}
