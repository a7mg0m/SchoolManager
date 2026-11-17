using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>A school event that concerns a parent's child (parent app). Targets are not exposed.</summary>
    public class ChildEventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly EventDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string? ImageUrl { get; set; }
        /// <summary>Who the event is for: All = 1, Stage = 2, Grade = 3, Section = 4, Student = 6</summary>
        public EventForEnum EventFor { get; set; }
    }
}
