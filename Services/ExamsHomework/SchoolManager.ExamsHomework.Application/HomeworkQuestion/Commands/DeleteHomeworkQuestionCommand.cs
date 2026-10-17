using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record DeleteHomeworkQuestionCommand(Guid Id) : ICommand;
}
