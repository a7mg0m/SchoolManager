using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record UpdateHomeworkQuestionCommand(
        Guid Id,
        string QuestionHeader,
        List<string>? Questions,
        string? ImageUrl,
        Guid UpdatedBy
    ) : ICommand;
}
