using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record CreateHomeworkQuestionCommand(
        Guid HomeworkId,
        string QuestionHeader,
        List<string>? Questions,
        string? ImageUrl
    ) : ICommand<Guid>;
}
