using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public record UpdateNotificationCommand(
        Guid Id,
        string Title,
        string Description,
        DateOnly SentOn,
        NotificationTypeEnum NotificationType,
        string? ImageUrl,
        List<TargetInputDto>? Targets,
        /// <summary>Academic year (the admin header's for new ones; kept as saved on edit)</summary>
        Guid AcademicYearId = default
    ) : ICommand;
}
