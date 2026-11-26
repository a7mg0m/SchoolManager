using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// End-of-year promotion of the students who passed their finals (approved on the admin's promotion page).
    /// Promote: they move to the target grade (its stage) and academic year, with no section (they are placed in a
    /// class later). Graduate (the last grade): they become Graduated. Only current students of FromGradeId move.
    /// </summary>
    public record PromoteStudentsCommand(
        List<Guid> StudentIds,
        Guid FromGradeId,
        Guid? TargetGradeId,
        Guid? TargetAcademicYearId,
        bool Graduate) : ICommand<int>;

    /// <summary>Body of PUT /api/student/promote</summary>
    public class PromoteStudentsDto
    {
        public List<Guid> StudentIds { get; set; } = new();
        public Guid FromGradeId { get; set; }
        public Guid? TargetGradeId { get; set; }
        public Guid? TargetAcademicYearId { get; set; }
        public bool Graduate { get; set; }
    }

    /// <summary>A promotion that cannot be done; Code is the error key the API answers (400)</summary>
    public class PromotionException(string code) : Exception(code)
    {
        public string Code { get; } = code;
    }

    public class PromoteStudentsCommandHandler(
        ICurrentUserService currentUser,
        IStudentRepository students,
        IGradeRepository grades,
        IAcademicYearRepository academicYears,
        IUnitOfWork unitOfWork) : ICommandHandler<PromoteStudentsCommand, int>
    {
        public async Task<int> Handle(PromoteStudentsCommand command, CancellationToken cancellationToken)
        {
            var ids = command.StudentIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (ids.Count == 0) throw new PromotionException("NoStudents");

            Grade? target = null;
            if (!command.Graduate)
            {
                if (command.TargetGradeId is not Guid gradeId || gradeId == Guid.Empty) throw new PromotionException("TargetGradeRequired");
                if (gradeId == command.FromGradeId) throw new PromotionException("SameGrade");
                target = await grades.GetByIdAsync(gradeId) ?? throw new PromotionException("TargetGradeRequired");

                if (command.TargetAcademicYearId is not Guid yearId || yearId == Guid.Empty
                    || await academicYears.GetByIdAsync(yearId) is null)
                    throw new PromotionException("TargetYearRequired");
            }

            var found = await students.SearchAsync(s => ids.Contains(s.Id));
            var audit = new AuditInfo(DateTime.Now, currentUser.UserId);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var moved = 0;
            foreach (var student in found.Where(s => s.Status == StudentStatus.Current && s.GradeId == command.FromGradeId))
            {
                if (command.Graduate)
                    student.ChangeStatus(StudentStatus.Graduated, today, null, audit);
                else
                    student.Promote(target!.StageId, target.Id, command.TargetAcademicYearId!.Value, audit);
                moved++;
            }

            await unitOfWork.SaveChangesAsync();
            return moved;
        }
    }
}
