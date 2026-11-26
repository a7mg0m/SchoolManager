using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Places students of a grade in one of its sections (SectionId null: takes them out of their section).
    /// Only current students of GradeId move; the section must be an active section of that grade.
    /// </summary>
    public record AssignSectionCommand(List<Guid> StudentIds, Guid GradeId, Guid? SectionId) : ICommand<int>;

    /// <summary>Body of PUT /api/student/assign-section</summary>
    public class AssignSectionDto
    {
        public List<Guid> StudentIds { get; set; } = new();
        public Guid GradeId { get; set; }
        public Guid? SectionId { get; set; }
    }

    public class AssignSectionCommandHandler(
        ICurrentUserService currentUser,
        IStudentRepository students,
        ISectionRepository sections,
        IUnitOfWork unitOfWork) : ICommandHandler<AssignSectionCommand, int>
    {
        public async Task<int> Handle(AssignSectionCommand command, CancellationToken cancellationToken)
        {
            var ids = command.StudentIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (ids.Count == 0) throw new PromotionException("NoStudents");

            var sectionId = command.SectionId is Guid id && id != Guid.Empty ? id : (Guid?)null;
            if (sectionId is Guid sid)
            {
                var section = await sections.GetByIdAsync(sid);
                if (section is null || !section.IsActive || section.GradeId != command.GradeId)
                    throw new PromotionException("SectionNotOfGrade");
            }

            var found = await students.SearchAsync(s => ids.Contains(s.Id));
            var audit = new AuditInfo(DateTime.Now, currentUser.UserId);
            var moved = 0;
            foreach (var student in found.Where(s => s.Status == StudentStatus.Current && s.GradeId == command.GradeId))
            {
                student.AssignSection(sectionId, audit);
                moved++;
            }

            await unitOfWork.SaveChangesAsync();
            return moved;
        }
    }
}
