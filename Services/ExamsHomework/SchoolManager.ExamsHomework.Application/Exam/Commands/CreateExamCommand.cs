using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public record CreateExamCommand(
        Guid SubjectId,
        Guid StageId,
        Guid GradeId,
        Guid? SectionId,
        Guid AcademicYearId,
        Guid SemesterId,
        ExamType ExamType,
        DateOnly ExamDate,
        TimeOnly StartTime,
        TimeOnly EndTime,
        double Duration,
        double Weight,
        double SuccessScore,
        string? AnswerModelUrl,
        int? Month = null,
        /// <summary>Teacher adding the exam from the teacher app (null: added by the school)</summary>
        Guid? TeacherId = null
    ) : ICommand<Guid>;
}
