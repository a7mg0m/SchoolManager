using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record CreateExamResultCommand(
        Guid ExamId,
        Guid StudentId,
        double Score,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
