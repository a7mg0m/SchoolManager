using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>A notification for a parent's child or a teacher (parent / teacher apps). Targets are not exposed.</summary>
    public class MyNotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly SentOn { get; set; }
        /// <summary>Homework = 1, Exam = 2, ExamResult = 3, Event = 4, Message = 5</summary>
        public NotificationTypeEnum NotificationType { get; set; }
        public string? ImageUrl { get; set; }
        /// <summary>For the whole school (else for the child's grade / class / the child, or for this teacher)</summary>
        public bool ForEveryone { get; set; }
    }
}
