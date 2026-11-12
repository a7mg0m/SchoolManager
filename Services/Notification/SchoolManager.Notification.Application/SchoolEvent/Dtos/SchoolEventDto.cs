using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class SchoolEventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly EventDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string? ImageUrl { get; set; }
        public EventForEnum EventFor { get; set; }
        public Guid AcademicYearId { get; set; }
        public Guid SemesterId { get; set; }
        public DateTime CreatedOn { get; set; }
        public List<EventTargetDto> Targets { get; set; } = new();
    }
}
