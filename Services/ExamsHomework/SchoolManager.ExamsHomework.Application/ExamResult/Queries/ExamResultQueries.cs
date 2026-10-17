using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record GetAllExamResultsQuery() : IQuery<List<ExamResultDto>>;

    public record GetExamResultByIdQuery(Guid Id) : IQuery<ExamResultDto?>;

    public record GetExamResultsByExamIdQuery(Guid ExamId) : IQuery<List<ExamResultDto>>;
}
