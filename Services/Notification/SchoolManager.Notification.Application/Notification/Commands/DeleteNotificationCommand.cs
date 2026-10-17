using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
    public record DeleteNotificationCommand(Guid Id) : ICommand;
}
