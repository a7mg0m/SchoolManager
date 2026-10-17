using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record DeleteHomeworkCommand(Guid Id) : ICommand;
}
