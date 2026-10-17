using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class CreateNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly SentOn { get; set; }
        public NotificationTypeEnum NotificationType { get; set; }
        public string? ImageUrl { get; set; }
        public List<TargetInputDto>? Targets { get; set; }
    }
}
