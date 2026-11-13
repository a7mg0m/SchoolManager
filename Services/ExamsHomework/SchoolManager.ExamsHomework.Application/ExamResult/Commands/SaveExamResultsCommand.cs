using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>
    /// Creates, updates or removes the results of one exam in a single transaction.
    /// Returns null when the exam does not exist.
    /// </summary>
    public record SaveExamResultsCommand(
        Guid ExamId,
        List<StudentScoreDto> Results
    ) : ICommand<SaveExamResultsSummary?>;
}
