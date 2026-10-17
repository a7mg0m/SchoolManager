using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class CreateSchoolEventDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly EventDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string? ImageUrl { get; set; }
        public EventForEnum EventFor { get; set; }
        public Guid AcademicYearId { get; set; }
        public Guid SemesterId { get; set; }
        public List<TargetInputDto>? Targets { get; set; }
    }
}
