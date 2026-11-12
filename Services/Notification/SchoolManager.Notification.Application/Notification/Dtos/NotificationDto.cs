using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly SentOn { get; set; }
        public NotificationTypeEnum NotificationType { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedOn { get; set; }
        /// <summary>Empty = the whole school.</summary>
        public List<NotificationTargetDto> Targets { get; set; } = new();
    }
}
