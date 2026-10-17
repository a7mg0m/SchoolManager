using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public record UpdateExamCommand(
        Guid Id,
        Guid SubjectId,
        Guid GradeId,
        Guid SectionId,
        Guid AcademicYearId,
        Guid SemesterId,
        ExamType ExamType,
        DateOnly ExamDate,
        TimeOnly StartTime,
        TimeOnly EndTime,
        double Duration,
        string? AnswerModelUrl,
        Guid UpdatedBy
    ) : ICommand;
}
