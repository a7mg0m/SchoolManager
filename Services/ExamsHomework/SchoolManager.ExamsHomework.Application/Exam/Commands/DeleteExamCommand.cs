using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record DeleteExamCommand(Guid Id) : ICommand;
}
