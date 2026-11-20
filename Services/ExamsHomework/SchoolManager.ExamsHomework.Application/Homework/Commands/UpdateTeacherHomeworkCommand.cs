using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>
    /// A teacher edits one of their homework: title, description, due date and the questions
    /// (the questions are replaced). Subject, grade, year and semester stay as created.
    /// Returns false when the homework does not exist or is not the teacher's.
    /// </summary>
    public record UpdateTeacherHomeworkCommand(
        Guid Id,
        Guid TeacherId,
        string Title,
        string Description,
        DateOnly DueDate,
        IReadOnlyList<HomeworkQuestionItemDto> Questions
    ) : ICommand<bool>;
}
