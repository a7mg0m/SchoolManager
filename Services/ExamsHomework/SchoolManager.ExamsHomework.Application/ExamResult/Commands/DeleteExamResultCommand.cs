using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record DeleteExamResultCommand(Guid Id) : ICommand;
}
