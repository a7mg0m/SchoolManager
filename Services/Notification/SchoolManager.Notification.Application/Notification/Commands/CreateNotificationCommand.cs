using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public record CreateNotificationCommand(
        string Title,
        string Description,
        DateOnly SentOn,
        NotificationTypeEnum NotificationType,
        string? ImageUrl,
        List<TargetInputDto>? Targets,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
