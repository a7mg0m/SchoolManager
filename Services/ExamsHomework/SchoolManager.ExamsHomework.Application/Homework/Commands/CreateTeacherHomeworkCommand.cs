using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>A homework created by a teacher in the teacher app (ids checked by the controller first).</summary>
    public record CreateTeacherHomeworkCommand(
        Guid TeacherId,
        Guid SubjectId,
        Guid GradeId,
        Guid AcademicYearId,
        Guid SemesterId,
        string Title,
        string Description,
        DateOnly DueDate,
        IReadOnlyList<HomeworkQuestionItemDto> Questions
    ) : ICommand<Guid>;
}
