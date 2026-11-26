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

            // No academic-year filter: a student has one current enrolment (grade / class), and the notification's year
            // (e.g. the homework's) may differ from the students' recorded year — filtering on it left classes without parents
            var found = await students.SearchAsync(s => s.IsActive && (
                gradeIds.Contains(s.GradeId) || (s.SectionId != null && sectionIds.Contains(s.SectionId.Value)) || studentIds.Contains(s.Id)));

            return found.Select(s => s.ParentId).Where(id => id != Guid.Empty).Distinct().ToList();
        }
    }
}
