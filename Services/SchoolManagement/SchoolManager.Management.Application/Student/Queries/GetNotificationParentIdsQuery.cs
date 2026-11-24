using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Push notifications (Notification service): the parents of the active students in these grades / classes
    /// (of one academic year when given) and of these students. Parent ids are their Identity user ids.
    /// </summary>
    public record GetNotificationParentIdsQuery(
        Guid? AcademicYearId,
        IReadOnlyCollection<Guid> GradeIds,
        IReadOnlyCollection<Guid> SectionIds,
        IReadOnlyCollection<Guid> StudentIds) : IQuery<List<Guid>>;

    public class GetNotificationParentIdsQueryHandler(IStudentRepository students)
        : IQueryHandler<GetNotificationParentIdsQuery, List<Guid>>
    {
        public async Task<List<Guid>> Handle(GetNotificationParentIdsQuery query, CancellationToken cancellationToken)
        {
            var gradeIds = query.GradeIds.Distinct().ToList();
            var sectionIds = query.SectionIds.Distinct().ToList();
            var studentIds = query.StudentIds.Distinct().ToList();
            if (gradeIds.Count + sectionIds.Count + studentIds.Count == 0)
                return new List<Guid>();

            var yearId = query.AcademicYearId;
            var found = await students.SearchAsync(s => s.IsActive && (
                ((gradeIds.Contains(s.GradeId) || sectionIds.Contains(s.SectionId)) && (yearId == null || s.AcademicYearId == yearId))
                || studentIds.Contains(s.Id)));

            return found.Select(s => s.ParentId).Where(id => id != Guid.Empty).Distinct().ToList();
        }
    }
}
