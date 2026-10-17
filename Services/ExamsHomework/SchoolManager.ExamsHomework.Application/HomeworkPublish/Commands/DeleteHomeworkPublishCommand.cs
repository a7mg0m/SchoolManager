using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record DeleteHomeworkPublishCommand(Guid Id) : ICommand;
}
