using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record GetAllHomeworkQuestionsQuery() : IQuery<List<HomeworkQuestionDto>>;

    public record GetHomeworkQuestionByIdQuery(Guid Id) : IQuery<HomeworkQuestionDto?>;

    public record GetHomeworkQuestionsByHomeworkIdQuery(Guid HomeworkId) : IQuery<List<HomeworkQuestionDto>>;
}
