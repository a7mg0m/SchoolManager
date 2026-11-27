using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>Home dashboard: per exam with results, the count, passes and average (instead of every result)</summary>
    public record GetExamResultSummariesQuery : IQuery<List<ExamResultSummary>>;

    public class GetExamResultSummariesQueryHandler(IExamRepository exams) : IQueryHandler<GetExamResultSummariesQuery, List<ExamResultSummary>>
    {
        public Task<List<ExamResultSummary>> Handle(GetExamResultSummariesQuery query, CancellationToken cancellationToken)
            => exams.GetResultSummariesAsync(cancellationToken);
    }
}
