using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public record UpdateExamCommand(
        Guid Id,
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
        /// <summary>Month of a Monthly exam (1..12); ignored for the other types</summary>
        int? Month = null,
        /// <summary>Second final: the Final exam it is the resit of</summary>
        Guid? OriginalExamId = null
    ) : ICommand;
}
