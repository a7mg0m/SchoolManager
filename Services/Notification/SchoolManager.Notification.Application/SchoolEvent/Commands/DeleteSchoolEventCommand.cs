using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
    public record DeleteSchoolEventCommand(Guid Id) : ICommand;
}
