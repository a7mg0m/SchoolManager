using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record UpdateExamResultCommand(
        Guid Id,
        Guid StudentId,
        double Score,
        Guid UpdatedBy
    ) : ICommand;
}
